# PAKET GRID-1 — Ortak ızgara + kişiye/ekrana özel kalıcı düzen · uygulama raporu

**Tarih:** 08.10.2026 · **Rol:** Taşkın (backend) + Ertuğ (frontend) · **Şerit:** t1 (API 5160 · vekil 5161 · CDP 9700 · `kuyumcu_test_t1`)
**Kaynak:** `GIRDI-KURALLARI-ENVANTER-2026-10-08.md` § (E) P2 + § (G) G1 · **Commit:** YOK (kural).

## TUR 3 — doğrulayıcı bulgularının kapatılması (08.10, şerit t1)

### T3.1 Bulgu → yapılan
| Bulgu | Önem | Yapılan | Kanıt |
|---|---|---|---|
| 375 kartta telefon ile bakiye bitişik, etiket–değer çapraz | YÜKSEK | Kart kalıbı `izgara.css`'te: satır TEK sütun; birincil olmayan her hücre kendi satırında, içinde 2 sütun ızgara (`::before` etiket solda, değer sağa yaslı); tutar ile rozet dikey yığın. Cariler'e özgü kart kuralları (`ortak.css` #cari-liste-sar ≤560 bloğu, `tuval-panel-musteri.css` ≤768 musteri-tablo td/tr kuralları) KALDIRILDI — kimlik özgüllüğü ortak kalıbı eziyordu. | 375 × PRO/BASİT × açık/koyu: her kartta "aynı satırda iki hücre" 0, "etiket–değer kayık" 0 (5 kart × 4 kombinasyon); `t3-*-375.png`; GC02, GC04 |
| Sayısal başlık solda, değer sağda | YÜKSEK | Izgara `Tur ∈ {Tl, Para, Has, Gram, Adet}` için th ve td'ye `izgara-sayi` (sağa hiza); sıralama düğmesinde ok başlığın solunda (`row-reverse`) | 1440/1280 × 2 mod × 2 tema: tl-bakiye/has/döviz th text-align=right, td=right, **başlık sağ kenarı − değer sağ kenarı = 0 px** (12 ölçümün hepsi); GE17, GC05 |
| Tutarın sağ kenarı satırdan satıra kayıyor | YÜKSEK | Tutar hücresi sabit düzen: `.izgara-tutar` dikey (tutar üstte, rozet altta, nowrap); olcek.css (10g) sarma kuralı özgüllükle aşıldı; `.izgara-tutar-deger` tabular-nums | Her sayısal sütunda satırlar arası tutar sağ kenarı farkı **0 px**; `t3-PRO-aydinlik-1440.png` |
| Klavye taşıma sonrası odak BODY | ORTA | Taşımadan sonra `OnAfterRenderAsync` → `ElementReference.FocusAsync` ile aynı sütunun sıralama düğmesine (yoksa th); panel taşımasında aynı yön düğmesine (kilitlendiyse karşı yön). Sıralama düğmesi yazma sırasında `disabled` yerine `aria-disabled` (disabled odağı düşürüyordu; yazma sürerken tıklama zaten yok sayılır) | Canlı Chrome: Alt+→ sonrası `activeElement=#cari-liste-sirala-telefon`, **ikinci Alt+→ çalıştı** (telefon 2 sütun ilerledi, 2 PATCH), panelde `#cari-liste-saga-tur` odakta kaldı; GE22 |
| Sütun paneli sade değil | ORTA | Açıklama paragrafı kaldırıldı; taşınamaz "Eylemler" satırı panelden çıktı; kilit sebebi düğmenin yanında kilit simgesi + `title` + `aria-label` + `aria-describedby`; 375'te yön düğmeleri ↑/↓ ve erişilebilir adı "Yukarı/Aşağı taşı" (CSS ile yatay/dikey ad değişir); panel kompakt satırlı AÇILIR KUTU (tabloyu itmez) | Panelde `p`=0, eylem satırı yok; 375'te görünen ad "↓ Aşağı taşı: Telefon"; panel açılınca tablo üstü değişmedi (12/12); GE05, GE21, GC04 |
| Düzen değişikliğinde tablo kayıyor; ikinci dolu düğme | ORTA | Başarı bandı yerine araç satırında satır içi kısa onay (✓ simgesi `Ikon Onay` + "Kaydedildi", 2,5 sn); araç satırı tek satır, sabit min. yükseklik; "Göster" `btn-ikincil`, `aria-pressed` kaldırıldı | Canlı: Alt+→ ve Gizle sonrası **tablo kayması 0 px**; "Göster" zemini beyaz (ikincil), "+ Yeni Cari" tek birincil; `kirp-onay.png` |
| BASİT koyu 1280 eylem başlığı farklı ton; tablo kart kenarına yapışık | ORTA | Başlık zemini tek belirteç `--izgara-baslik-zemin` (thead + yapışkan ilk th + olcek.css yapışkan eylem th'si); BASİT tuvali onu `--yuzey-govde`'ye bağlar. İlk/son hücreye ≥16 px iç boşluk (≥561 px) | th eylem iç gölgesi = thead rengi (BASİT koyu: rgb(14,18,25) ikisi de); kart kenarı → ilk içerik 17 px, son içerik → kart kenarı 17 px |
| /tercihler/benim açılışta 3-4 kez | DÜŞÜK×2 | Sayfa TEK `IzgaraTercihIstemcisi` tutar ve ızgaraya `TercihIstemcisi` parametresiyle verir; istemci başarılı okumayı önbelleğe alır, kendi PATCH/DELETE'iyle günceller (hata önbelleğe alınmaz) | Canlı SPA açılış: **GET /tercihler/benim = 1**; liste boşalıp dolunca yine 1; GE18 (yazma sonrası yeniden kurulumda SON düzen) |
| Hata bandında ham istisna metni | DÜŞÜK | Yalnız `ApiHata` mesajı; diğer istisnada sözlükten "Bağlantı kurulamadı, lütfen tekrar deneyin." | GE19 (HttpRequestException "TypeError: Failed to fetch" bantta YOK), GE20 |
| BASİT 1280 başlık zemini eylem sütununu kaplamıyor | DÜŞÜK | Başlık zemini maddesiyle kapandı | ölçüm tablosu thZemin |
| (canlıda YENİ bulundu) BASİT↔PRO geçişinde telefon td'si class/data-kolon/data-etiket'i kaybediyordu → 375 BASİT kartta "Telefon" etiketi yok | — | Iki hücre dalı (tıklanabilir/değil) aynı `@key` taşıyordu; anahtarlar dala göre ayrıştı `(Anahtar, true/false)` | Canlı mod geçişi sonrası td öznitelikleri tam; GE23 (davranış), GS01 (kaynak kilidi — bUnit tarayıcı DOM yamasını taklit etmediği için M3-05'i GE23 yakalamadı, GS01 yakaladı) |

Sayısal-ağır örnek tablo: göç YAPILMADI; Cariler PRO tablosu canlı test verisiyle (düzeltme fişi: TL −987.654,32 / 1.234.567,89, has −12,5000 / 150,1234, döviz 250 USD / −40 EUR) **üç sayısal sütunla** (TL Bakiye · Has Gram · Döviz Bakiye) ölçüldü.

### T3.2 Ölçüm (gerçek başsız Chrome, CDP 9700, vekil 5161, `sikisma-olc.js`; görüntüler Read ile görüldü)
| Mod | Tema | Genişlik | tr | K1 | K3 | K5 TRY | K9 | K11 | yatay | K2 | th/td sağ fark | tutar kenar farkı | panel açık K3/K9/yatay | tablo kayması (panel) |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| PRO | açık | 1440 | table-row | 0 | 0/0 | 0 | 0 | 0 | 0 | 98 | 0/0/0 | 0/0/0 | 0/6*/0 | 0 |
| PRO | açık | 1280 | table-row | 0 | 0/0 | 0 | 0 | 0 | 0 | 98 | 0/0/0 | 0/0/0 | 0/6*/0 | 0 |
| PRO | açık | 375 | grid (kart) | 0 | 0/0 | 0 | 0 | 0 | 0 | 97 | — | — | 0/6*/0 | 0 |
| PRO | koyu | 1440 | table-row | 0 | 0/0 | 0 | 0 | 0 | 0 | 98 | 0/0/0 | 0/0/0 | 0/6*/0 | 0 |
| PRO | koyu | 1280 | table-row | 0 | 0/0 | 0 | 0 | 0 | 0 | 98 | 0/0/0 | 0/0/0 | 0/6*/0 | 0 |
| PRO | koyu | 375 | grid (kart) | 0 | 0/0 | 0 | 0 | 0 | 0 | 97 | — | — | 0/6*/0 | 0 |
| BASİT | açık | 1440 | table-row | 0 | 0/0 | 0 | 0 | 0 | 0 | 98 | 0/0/0 | 0/0/0 | 0/6*/0 | 0 |
| BASİT | açık | 1280 | table-row | 0 | 0/0 | 0 | 0 | 0 | 0 | 98 | 0/0/0 | 0/0/0 | 0/6*/0 | 0 |
| BASİT | açık | 375 | grid (kart) | 0 | 0/0 | 0 | 0 | 0 | 0 | 97 | — | — | 0/6*/0 | 0 |
| BASİT | koyu | 1440 | table-row | 0 | 0/0 | 0 | 0 | 0 | 0 | 98 | 0/0/0 | 0/0/0 | 0/6*/0 | 0 |
| BASİT | koyu | 1280 | table-row | 0 | 0/0 | 0 | 0 | 0 | 0 | 98 | 0/0/0 | 0/0/0 | 0/6*/0 | 0 |
| BASİT | koyu | 375 | grid (kart) | 0 | 0/0 | 0 | 0 | 0 | 0 | 97 | — | — | 0/6*/0 | 0 |
(th/td ve tutar kenar farkı sütunları: TL / Has / Döviz.) \* Panel açıkken K9=6: ölçer AÇILIR KUTUNUN altındaki tablo hücrelerini "binen" sayıyor (örnek: `#cari-liste-sutunlar-baslik ⟂ td.hucre-vurgu`). Bilinçli tasarım: bulgu "panel tabloyu itiyor" idi; panel opak, z-index'li, Esc ile kapanan açılır kutudur. Panel KAPALIYKEN K9=0 (12/12). Hakem tablo-itmeyi değil üst üste çizimi tercih etmezse panel yeniden akışa alınabilir (tek CSS satırı).
375 kart geometrisi (her kombinasyonda 5 kart): aynı satırda iki hücre 0 · etiket–değer kayık 0 · etiketli hücre display=grid.
Görüntüler: `/private/tmp/kuyumcu-serit/GRID-1/tur3/tarayici/foto/` — `t3-<MOD>-<tema>-<w>[-panel].png`, `t3-klavye1-PRO-1440.png`, `t3-panel-gizle-PRO-1440.png`, `kirp-onay.png`, `incele-basit-375.png`, `modgecis-basit-375.png`.

### T3.3 Canlı akışlar (5160, test kiracısı `PAKETGRID-1-TEST …` 01M4CQPEXFQRYKE4HB1DEY17HB; A=SAHİP, B=MÜDÜR)
| Adım | Beklenen | Gerçek |
|---|---|---|
| SPA ile /cariler açılış | tercih GET 1 | 1 (`GET /api/v1/tercihler/benim`, ardından tek `GET /api/v1/cari?limit=100&aktif=true`) |
| Liste boşalt-doldur (arama) | ek tercih GET yok | toplam yine 1 |
| Alt+→ ×2 (telefon) | odak korunur, 2 PATCH | odak `cari-liste-sirala-telefon` iki kez; 2 PATCH; tablo kayması 0 |
| A PATCH geçerli / 2001 karakter | 200 / 400 | 200 / 400 "Izgara tercihi çok uzun: en çok 2000 karakter." |
| B GET A'nın anahtarı | görünmez | False |
| B DELETE aynı anahtar | 200, A'nınki durur | 200, A'nınki duruyor: True |
| A çıkış → yeniden giriş | aynı düzen | telefon·ad·tür·tl-bakiye·döviz·eylem (has gizli), tl-bakiye descending — çıkış öncesiyle AYNI |
| B açılış | varsayılan | ad·tür·telefon·tl-bakiye·has·döviz·eylem; "Varsayılana dön" kilitli + "Düzen zaten varsayılan" |
| A'nın kaydı SQL ile bozuldu (`{bozuk-eski-kayit`) | varsayılan, hata yok | varsayılan; izgaraHata=false, bantHata=0, blazorHata=false; "Varsayılana dön" → satır 0 |
SQL: `denetim_kayitlari` (varlik_turu=KullaniciTercihi): EKLEME "Arayüz tercihi güncellendi: grid:cariler:cari-liste" ×8, SILME ×2. Temizlik: `zsh kiraci-temizle.sh 01M4CQPEXFQRYKE4HB1DEY17HB --uygula` → kiracı satırı 0; davet satırı silindi (0); `tenants` 16 → 17 (benim) → **16 + 1 başka şerit** (sayım 17: aradaki tek yeni kiracı `PAKETGIRDI2-HAKEM-TEST …` 03:27 UTC, GIRDI-AILESI-2 hakem şeridinin; ona dokunulmadı). API/vekil durduruldu; 5160/5161/9700 dinleyen yok; fikstürler `printf ''` ile boşaltıldı.

### T3.4 Mutasyon (yedek → tek eşleşme → filtreli test → geri koy → `cmp`)
| Kod | Değişiklik | Kırmızı test | cmp |
|---|---|---|---|
| M3-01 | Sayısal başlığa `izgara-sayi` verilmez | GE17 (PRO+BASİT) | AYNI |
| M3-02 | Tercih önbelleği kapatıldı (her okuma GET) | GE18 | AYNI |
| M3-03 | Bantta ham `ex.Message` | GE19 | AYNI |
| M3-04 | Klavye taşımasında odak isteği yok | GE22 | AYNI |
| M3-05 | İki hücre dalının `@key`'i aynı | GS01 (GE23 bUnit'te yakalamaz — not) | AYNI |
| M3-06 | Kart satırı 2 sütuna döndü | GC04 | AYNI |
| M3-07 | Eylem satırı panele geri | GE05 | AYNI |
| M3-08 | Tutar+rozet yan yana (row) | GC05 | AYNI |
| M3-09 | Yazma sonrası önbellek güncellenmez | GE18 | AYNI |
Son mutasyondan sonra temiz `dotnet build Kuyumcu.sln`: 0 hata.

### T3.5 Derleme · denetim · tam süit
- `dotnet build Kuyumcu.sln`: 0 hata; uyarı yalnız başka dosyalarda (SUBEKAPSAMASinirTestleri:49, KesifP2Testleri:455, SonKalanlarTestleri:584/813 — bu paketten önce de vardı); bu paketin dosyalarında 0 uyarı.
- `denetim.sh`: tek ✗ **P-30 DEMO_INSAN_SURESI** (kapsam dışı, kurucu kalemi); `✓ GRID-1: HAM_TABLO_ORTAK_GRID_DISI 95`. Ara koşumda benim ürettiğim `✗ Layout+Bilesenler emoji ikon 1` (onaydaki ✓ karakteri) `Ikon Onay` ile kapandı.
- Grid filtresi: 113/113 yeşil (önce tur 2: 63 + tur 2 ekleri; tur 3 yeni: GE17×2, GE18–GE23, GC04, GC05, GS01 = 11 koşum). İlgili eski sınıflar (Cari*, Musteri*, MobilParite, S1iGorsel, OlcekTek, KesifP7, S31PanelSade, Toplu1, TercihKatlama): 954/955 (1 atlandı, önceden atlanan).
- Tam süit (BİR kez, `--results-directory /private/tmp/kuyumcu-serit/trx-pGRID-1-tur3`): **10943 · 10941 geçti · 0 başarısız · 2 atlandı** → yeni kırmızı 0.

### T3.6 Tur 3'te değişen dosyalar
`Bilesenler/Grid/Izgara.razor`, `Bilesenler/Grid/IzgaraTercihIstemcisi.cs`, `Kuyumcu.Core/I18n/Sozlukler.Izgara.cs` (alanlar: YukariTasi/AsagiTasi/EnBasta/EnSonda/GenelYazmaHatasi eklendi; SutunlarIpucu/SabitSonda/EnSolda/EnSagda/SiralamaArtanDurum/SiralamaAzalanDurum kaldırıldı — başka kullanıcı yok), `wwwroot/css/izgara.css`, `Pages/Cariler.razor` (yalnız liste ızgarası: `TercihIstemcisi` parametresi, tutar hücrelerine `izgara-tutar` sınıfı + değer kutusu; id'ler aynı), **`wwwroot/css/ortak.css`** (Cariler ≤560 kart bloğu kaldırıldı) ve **`wwwroot/css/tuval-panel-musteri.css`** (≤768 musteri-tablo kart kuralları kaldırıldı + `--izgara-baslik-zemin` bağı) — DOSYA ALANI dışı; gerekçe: bulgu "tek kart kalıbında (css/izgara.css)" diyordu ve bu iki dosyanın kimlik/sınıf özgüllüğü ortak kalıbı ezerek bulgunun KAYNAĞIYDI; yalnız Cariler listesinin kart kuralları silindi. Testler: `GridTestleri.cs`, `Toplu1Testleri.cs` (Ortak_css_TOPLU1: silinen kuralın varlığı yerine ortak kalıba devri denetlenir).
Yan etki notu: BASİT 561–768 px aralığında Cariler artık kart değil kaydırılabilir tablodur (ortak kalıbın kırılımı 560). Ölçüm genişliklerinin (375/1280/1440) dışında; göç paketlerinde tablet kırılımı ortak kalıpta tek yerden ele alınmalı.
**Dokunulan sayfalar (DoD EK 2):** `Pages/Cariler.razor` (aynı paket, aynı liste ızgarası — ikinci dalga değil, doğrulayıcı turu).

### T3.7 Açık kalanlar
- Panel açıkken ölçer K9=6 (açılır kutu tablonun üstünde; tasarım gereği — hakem kararı).
- Kabuk düzeyinde `MenuTercihServisi` ve `KabukServisi` uygulama AÇILIŞINDA ayrıca `/tercihler/benim` okur (tam sayfa yüklemede); bu dosyalar paket alanı dışında. Sayfa/ızgara yolu tek GET'e indi (SPA gezinmesinde ölçüldü: 1).
- BASİT 561–768 px tablet kırılımı (yukarıda).

## 1. Yapılanlar

| Katman | Dosya | Ne |
|---|---|---|
| Kural (tek kaynak) | `Kuyumcu.Core/Domain/IzgaraTercihi.cs` (yeni) | `IzgaraTercihKurali`: anahtar deseni `^grid:[a-z0-9-]+:[a-z0-9-]+$` + bilinen ekran listesi (`cariler`) + 60 karakter (anahtar sütunu VARCHAR(60)); değer `{"s","k","g","o"}` ayrıştırma/doğrulama (bilinmeyen alan, yinelenen kolon, geçersiz yön, eksik sürüm → geçersiz), en çok 2000 karakter, kullanıcı başına en çok 200 ızgara anahtarı; `Yaz()`. Sunucu ve istemci AYNI sınıfı çağırır. |
| Sunucu | `Kuyumcu.Api/Modules/Tercih/TercihModulu.cs` | Beyaz liste → `TercihAnahtarlari.Gecerli` (sabit liste VEYA ızgara deseni). PATCH'te ızgara değeri yazılmadan doğrulanır (400, Türkçe sebep). 200 anahtar sınırı SQL `COUNT` ile (sınırsız ToListAsync yok). DELETE ızgara anahtarını kabul eder ("Varsayılana dön"). Denetim izi mevcut `DenetimServisi.YazmaKaydet` ile (yeniden yazılmadı). Migration YOK. |
| Sunucu sıralama | `Kuyumcu.Api/Modules/Cari/CariSiralama.cs` (yeni) + `CariModulu.cs` (Listele imzası + controller 2 parametre) | `GET /cari?sirala=ad|tur|telefon|tl-bakiye&yon=artan|azalan`; bilinmeyen ad/yön ya da alansız yön → 400 (sessiz varsayılan yok). Varsayılan eskisi gibi ad (+ id kararlılık). Sınır sıralamadan SONRA uygulanır. Has bilinçli olarak sıralanmaz (listede EMANETSİZ hesaplanır — ham sütun sırası ekrandaki sayıyla çelişirdi). |
| İstemci bileşen | `Kuyumcu.Web/Bilesenler/Grid/Izgara.razor`, `IzgaraKolonu.cs`, `IzgaraDuzeni.cs`, `IzgaraTercihIstemcisi.cs` (yeni) | Kolon sırası: başlığı sürükle (mevcut `js/siralistesi.js` köprüsü — fare+dokunmatik, yeni betik YOK) · başlıkta Alt+←/→ · "Sütunlar" panelinde Sola/Sağa düğmeleri. Göster/gizle (zorunlu kolon kilitli + "Her zaman görünür", eylemler "hep en sonda"). Başlık sıralaması artan→azalan→varsayılan, `aria-sort`, SUNUCUYA gider (`SiralamaDegisti`). Tercih okuma fail-safe (bozuk/eski sürüm → varsayılan, hata yok, tercih silinmez), yazma hatasında geri alma + bant. "Varsayılana dön" (DELETE; kayıt yokken kilitli + sebep). Her hücre `data-etiket`. DI kaydı gerekmez (ApiIstemci'yi sarar) → başka ekran testleri kırılmaz. |
| Sözlük | `Kuyumcu.Core/I18n/Sozlukler.Izgara.cs` (yeni) | TR+EN konumsal kayıt (eksik dil = derleme hatası). |
| CSS | `wwwroot/css/izgara.css` (yeni, tek dosya) + `index.html` 1 bağlantı | Araç çubuğu, panel, sıralama oku (CSS `::after`, başlık metni yalnız başlık), TEK kart kalıbı ≤560 px (`:where()` ile özgüllük 0 — göçmemiş sayfaya özgü kural kazanır), kartta yapışkan ilk sütun kapatıldı. Sabit renk yok. |
| Örnek ekran | `Pages/Cariler.razor` YALNIZ liste tablosu | Ham `<table>` → `<Izgara>`; hücre içerikleri `CariHucre*` render yöntemlerine AYNEN taşındı; id/sınıf sözleşmesi korundu (`#cari-liste-sar`, `#c-satir-*`, `#c-eylem-*`, `#c-telefon-*` …). `Yukle()` sıralamayı `sirala=&yon=` ile gönderir. |
| Denetim | `dotnet/denetim.sh` YALNIZ `HAM_TABLO_ORTAK_GRID_DISI` bloğu | Cırcır: taban 96 (envanter) → 95 (Cariler taşındı); yazdırma belgesi istisnası ayrıca sayılır (tavan 4); ortak ızgarada tam 1 `<table>`; ölçülemezse ✗. |
| Test | `Kuyumcu.Tests/GridTestleri.cs` (yeni) + `MobilPariteTestleri.cs`, `S1iGorselSigma2Testleri.cs` (Cariler kaynak-metin denetimi ızgaraya uyarlandı) | 4 sınıf, 63 koşum (aşağıda). |

## 2. Sapmalar ve gerekçeleri
1. **`CariModulu.cs` + yeni `CariSiralama.cs`** DOSYA ALANI listesinde yoktu. Kabul ölçütü 1 ("sıralama sunucudan gelir") ve § G1 ("liste uçlarına `sirala=&yon=` eklenir, bilinmeyen ad 400") bunu zorunlu kılıyor; değişiklik Listele imzasına iki isteğe bağlı parametre + tek satır (`CariSiralama.Uygula`). Varsayılan davranış korunur (sirala yoksa ad sırası).
2. **`Kuyumcu.Core/Domain/IzgaraTercihi.cs`** (yeni dosya, alan dışı): kuralın sunucu ve istemcide TEK sınıf olması için (kurucu ilkesi "bir yerde var diğerinde yok olmasın"). Başka şeridin dosyasına dokunulmadı (GIRDI-AILESI-2'nin `Core/Domain/Girdi/` dizini değil).
3. **`index.html`** tek `<link>` satırı (yeni CSS dosyası yüklenmezse kalıp çalışmaz); `olcek.css`→`dugme.css`→`ortak.css` zinciri bozulmadı (OlcekTek OL5 yeşil).
4. **`_Imports.razor`'a dokunulmadı**: Cariler'de tam ad (`Kuyumcu.Web.Bilesenler.Grid.Izgara`) kullanıldı; göç paketleri isterse `@using` ekler.
5. **HTML5 `draggable` kullanılmadı**: dokunmatikte çalışmaz; mevcut `kuyumcuSira` (Pointer Events) köprüsü yeniden kullanıldı (yeni betik yok, CSP istisnası yok). JS geri çağrısı Blazor olayı olmadığından açık `StateHasChanged` eklendi (canlıda bulundu, GE13 + M-GRID1-09 ile kilitlendi).
6. **`HucreTikla` `EventCallback<T>`** (Func değil): Func'ta tıklama yalnız ızgarayı yeniden çiziyor, sayfanın açtığı panel görünmüyordu (TM1 testleri yakaladı; düzeltildi).
7. **Alt+← tarayıcı "geri"si**: Alt basılıyken `th` üzerinde `preventDefault` (yalnız Alt basılıyken) — sayfa geri gitmez.
8. **BASİT modda "Sütunlar" düğmesi** küçük ikincil düğme, panel KAPALI gelir (zorunlu önce kuralı). Tercih ekran başına tektir; BASİT/PRO kolon kümesi farkı (Tür/Durum PRO'da sütun) dayanıklılık kuralıyla çözülür.
9. **Katlanir bileşeni gerekmedi** (panel aç/kapa ızgaranın kendi düğmesi).

## 3. DoD sözleşmesi
| Kayıt | Oluşturma | Düzenleme | Silme | Yetki | Denetim | Ekranda |
|---|---|---|---|---|---|---|
| Izgara düzeni (`kullanici_tercihleri`, anahtar `grid:<ekran>:<grid-id>`) | PATCH `/tercihler/benim` | PATCH (gönderilmeyen anahtar KORUNUR, `null` SİLER — mevcut sözleşme) | DELETE `/tercihler/benim/{anahtar}` (arayüz tercihi; finansal değil → hard delete serbest) | `[HerkeseAcik]` + kimlik JETONDAN (başkasının satırı okunamaz/yazılamaz/silinemez — GH05 + canlı) | `YazmaKaydet` ("Arayüz tercihi güncellendi: grid:…") — canlı SQL'de görüldü | Sola/Sağa, Gizle/Göster, sıralama, "Varsayılana dön" |
| Cari listesi sıralaması | — (salt okuma parametresi) | — | — | mevcut cari liste yetkisi | mevcut liste okuma izi | başlık düğmesi |
Yeni tablo/varlık YOK → migration gerekmedi.

## 4. Testler
Yeni: `GridTestleri.cs` — **GridKuralTestleri** (GK01–GK05, 25 koşum), **GridDuzenTestleri** (GD01–GD10), **GridTercihHttpTestleri** (GH01–GH09; gerçek HTTP + SQL), **GridEkranTestleri** (GE01–GE13; bUnit /cariler). Grid filtresi: **63/63 yeşil** (temiz derleme sonrası; GK 25 + GD 10 + GH 15 + GE 13 koşum).
Etkilenen eski sınıflar (Cariler'i çizen 23 sınıf + CSS/index sınıfları + TercihKatlamaHttp): 302/302 + 150/150 yeşil (TM1'in 12 kırmızısı `EventCallback` düzeltmesiyle kapandı).

## 5. Mutasyon tablosu (yöntem: yedek → TEK eşleşmeli değişiklik → `dotnet test --filter Grid` → geri koy → `cmp`)
| Kod | Değişiklik | Kırmızı olan (kendi hedefi) | cmp |
|---|---|---|---|
| M-GRID1-01 | Sunucu ızgara değer doğrulaması kapatıldı | GH02 (bozuk/yinelenen/yön), GH03 | AYNI |
| M-GRID1-02 | 200 anahtar sınırı +1 gevşetildi | GH04 | AYNI |
| M-GRID1-03 | Bilinen ekran listesi denetimi kaldırıldı | GK01, GH02, GH04, GH06 | AYNI |
| M-GRID1-04 | Değer sınırı 2000 → 4000 | GK05, GH03 | AYNI |
| M-GRID1-05 | Sürüm uyuşmazlığında varsayılana düşme kaldırıldı | GD05, GE03 | AYNI |
| M-GRID1-06 | Zorunlu kolon gizlenebilir | GD04 | AYNI |
| M-GRID1-07 | Bilinmeyen sıralama adı 400 yerine sessiz varsayılan | GH09 | AYNI |
| M-GRID1-08 | TL bakiye sıralama yönü ters | GH08 | AYNI |
| M-GRID1-09 | JS sürükle-bırak sonrası yeniden çizim kaldırıldı | GE13 | AYNI |
| M-GRID1-10 | Yazma hatasında geri alma kaldırıldı | GE10 | AYNI |
| M-GRID1-11 | Izgara sıralaması liste isteğine eklenmiyor | GE06, GE07 | AYNI |
| M-GRID1-12 | Bozuk kayıt "yok" sayılıyor (Varsayılana dön silemez) | GE03 | AYNI |
| M-GRID1-13 | `Cariler.razor` eski ham tabloya döndürüldü (denetim sayacı) | `✗ GRID-1: HAM_TABLO_ORTAK_GRID_DISI 96 (tavan 95 — ARTIŞ)` | AYNI |
⚠ Süreç notu (md.1): koşucu yedeği `copy2` ile (eski mtime) geri koyduğu için artımlı derleme M-04 ve M-08 sonrası değişikliği yeniden derlemedi; M-05…M-12 koşumlarında GK05/GH03/GH08 ek kırmızıları O ARTIKTIR (her mutasyonun kendi hedef testi ayrıca kırmızı). Tüm kaynaklar `touch` + temiz derleme sonrası grid filtresi 63/63 yeşil doğrulandı; kaynaklar `cmp` ile birebir.

## 6. Canlı doğrulama (5160, test kiracısı `PAKETGRID-1-TEST …` = 01M4CFVVE2XKD6Y2X58T3YW7DB; A=SAHIP, B=MUDUR)
### API (yanıt gövdeleri okundu)
| Adım | Beklenen | Gerçek | Kanıt |
|---|---|---|---|
| A PATCH geçerli ızgara tercihi | 200 | 200 | "Tercihler kaydedildi" |
| A PATCH bozuk JSON | 400 | 400 | "Geçersiz ızgara tercihi (grid:cariler:cari-liste): Izgara tercihi geçerli JSON değil." |
| A PATCH yinelenen kolon | 400 | 400 | "… 'k' içinde yinelenen kolon: ad." |
| A PATCH bilinmeyen ekran | 400 | 400 | "Bilinmeyen tercih anahtarı: grid:bilinmeyen:x" |
| A PATCH 2001 / 2000 karakter | 400 / 200 | 400 / 200 | "… en çok 2000 karakter." |
| A GET geri okuma | 200, aynı değer | 200, True | |
| B GET (A'nın tercihi) | görünmez | False | |
| B DELETE aynı anahtar | 200, A'nınki durur | 200, A'nınki duruyor: True | |
| `/cari?q=GRIDSIRA` (varsayılan) | ad | Alfa(0), Beta(500), Gama(50) | |
| `sirala=ad&yon=azalan` | Gama, Beta, Alfa | aynı | |
| `sirala=tl-bakiye&yon=azalan` / `artan` | Beta, Gama, Alfa / Alfa, Gama, Beta | aynı | veresiye ile 500/50 TL |
| `limit=1&sirala=tl-bakiye&yon=azalan` | Beta | Beta | sınır sıralamadan sonra |
| `sirala=has` · `yon=yukari` · yalnız `yon` | 400 ×3 | 400 ×3 | "Bilinmeyen sıralama alanı: has. Geçerli: ad, tur, telefon, tl-bakiye." … |
SQL: `kullanici_tercihleri` → `grid:cariler:cari-liste | grid1-a-…@test.local | 99 | {"s":1,"k":["telefon","ad","tur","tl-bakiye","has"],"g":["has"],"o":{"a":"tl-bakiye","y":"azalan"}}` (yalnız A'nın satırı). `denetim_kayitlari`: `EKLEME KullaniciTercihi "Arayüz tercihi güncellendi: grid:cariler:cari-liste"` ×2, `…grid:cariler:boyut` ×2, `SILME` ×1.

### Gerçek başsız Chrome (CDP 9700, vekil 5161, ekran görüntüleri Read ile görüldü)
| Senaryo | Sonuç |
|---|---|
| A açılış (kayıtlı: telefon başta, has gizli, TL azalan) | kolonlar telefon·ad·tür·tl-bakiye·eylem; satırlar Beta, Gama, Alfa; `aria-sort` tl-bakiye=descending |
| Fareyle sürükle (Tür → Telefon'un yerine) | tür·telefon·ad·… (tercihe yazıldı, hemen çizildi) |
| Klavye: Telefon başlık düğmesinde Alt+→ | tür·ad·telefon·… |
| Panel: Has göster, Ad gizle (KİLİTLİ, "Her zaman görünür"), Telefon gizle | tür·ad·tl-bakiye·has·eylem; "Sütun düzeni kaydedildi." |
| Ad başlığı sırala | Alfa, Beta, Gama; ad=ascending |
| ÇIKIŞ (jetonlar silindi, /giris) + YENİDEN GİRİŞ | aynı düzen ve sıralama geldi |
| B (başka kullanıcı) açılış | varsayılan ad·tür·telefon·tl-bakiye·has·eylem; "Varsayılana dön" kilitli + "Düzen zaten varsayılan" |
| A'nın kaydı SQL ile bozuldu (`{bozuk-eski-kayit`) | varsayılan düzen, hata bandı 0, blazor hatası yok; "Varsayılana dön" → DELETE → satır 0 |
| A'nın kaydı s=9 (başka sürüm) | varsayılan düzen, hata yok; Varsayılana dön çalıştı |

Ölçüm (`sikisma-olc.js`, panel kapalı ve AÇIK hâlde ayrı ayrı; /cariler):
| Mod | Tema | Genişlik | Kolonlar | tr | K1 | K3 taşan/kırık | K5 TRY | K9 binen | K11 | yatay | panel açık K3/K9/yatay |
|---|---|---|---|---|---|---|---|---|---|---|---|
| PRO | açık | 1440 | 5 | table-row | 0 | 0/0 | 0 | 0 | 0 | 0 | 0/0/0 |
| PRO | açık | 1280 | 5 | table-row | 0 | 0/0 | 0 | 0 | 0 | 0 | 0/0/0 |
| PRO | açık | 375 | 5 | **grid (kart)** | 0 | 0/0 | 0 | 0 | 0 | 0 | 0/0/0 |
| PRO | koyu | 1440 | 5 | table-row | 0 | 0/0 | 0 | 0 | 0 | 0 | 0/0/0 |
| PRO | koyu | 1280 | 5 | table-row | 0 | 0/0 | 0 | 0 | 0 | 0 | 0/0/0 |
| PRO | koyu | 375 | 5 | **grid (kart)** | 0 | 0/0 | 0 | 0 | 0 | 0 | 0/0/0 |
| BASİT | açık | 1440 | 4 | table-row | 0 | 0/0 | 0 | 0 | 0 | 0 | 0/0/0 |
| BASİT | açık | 1280 | 4 | table-row | 0 | 0/0 | 0 | 0 | 0 | 0 | 0/0/0 |
| BASİT | açık | 375 | 4 | **grid (kart)** | 0 | 0/0 | 0 | 0 | 0 | 0 | 0/0/0 |
| BASİT | koyu | 1440 | 4 | table-row | 0 | 0/0 | 0 | 0 | 0 | 0 | 0/0/0 |
| BASİT | koyu | 1280 | 4 | table-row | 0 | 0/0 | 0 | 0 | 0 | 0 | 0/0/0 |
| BASİT | koyu | 375 | 4 | **grid (kart)** | 0 | 0/0 | 0 | 0 | 0 | 0 | 0/0/0 |
K2 (en geniş blok) %97–98 → dar sütun yok. Görüntüler: `/private/tmp/kuyumcu-serit/GRID-1/tarayici/foto/` (`olcum-<MOD>-<tema>-<w>[-panel].png`, `A-*.png`, `B-varsayilan-PRO-1440.png`). İlk ölçümde 375 PRO kartında ilk hücrenin sağında yapışkan sütun gölgesi (dikey çizgi) GÖRÜLDÜ → `izgara.css`'te kart kipinde yapışkanlık kapatıldı, yeniden ölçüldü, çizgi yok.
Gözlem (benim değil, önceden var): BASİT koyu 1280'de başlık zemini eylem sütununun üstünde açık renk kalıyor — 07.10 TCKN doğrulama görüntüsünde (`dogrulama-pTCKN/olcum/cariler__BASIT_karanlik_1280.png`) aynı.

Temizlik: API (PID) ve vekil durduruldu, 5160/5161/9700 dinleyen yok. `zsh kiraci-temizle.sh 01M4CFVVE2XKD6Y2X58T3YW7DB --uygula` → tenant_id'li 75 tabloda 0 satır; davet satırı silindi (0); `tenants` 16 → 17 → **16**. Fikstür dosyaları `printf ''` ile boşaltıldı.

## 7. Derleme · denetim · tam süit
- `dotnet build Kuyumcu.sln`: **0 hata, 4 uyarı** — dördü de başka dosyalarda (`Dogrulayici/SUBEKAPSAMASinirTestleri.cs:49`, `KesifP2Testleri.cs:455`, `SonKalanlarTestleri.cs:584, 813`), bu paketten önce de vardı.
- `denetim.sh` (son kod): tek ✗ **P-30 DEMO_INSAN_SURESI** (kurucu kalemi A1, kapsam dışı — "P-30 hariç TEMİZ"); `✓ GRID-1: HAM_TABLO_ORTAK_GRID_DISI 95 (tavan 95, yalnız azalır; yazdırma belgesi 4)`. Ara koşumda görülen `Sınırsız ToListAsync 143/142` ve `CARI_BAKIYE_DOGRUDAN_YAZIM 1` benimdi (200-sınırı sayımı ToListAsync → COUNT; `TlBakiye = "tl-bakiye"` sabiti yalancı pozitif → `TlBakiyeSirasi`) — düzeltildi, son koşumda ✓. `EKRAN_SUNUCU_UZUNLUK_FARKI 170/169` ara koşumda başka şeridin (Ayarlar MFA alanları) işiydi, son koşumda ✓.
- Tam süit (bir kez, `--logger trx --results-directory /private/tmp/kuyumcu-serit/trx-pGRID-1`): **10804 test · 10800 geçti · 2 başarısız · 2 atlandı**. İki kırmızı BU PAKETİNDİ — /cariler kaynağını METİN olarak okuyan eski testler:
  - `MobilPariteCssTestleri.B9_Veri_tablolari_TEK_sarmalayici_kalibini_kullanir` (`<KartListe` Cariler.razor'da arıyordu; artık ızgaranın içinde) → test, ızgaraya geçen sayfada KartListe'yi `Izgara.razor`'da arayacak şekilde güncellendi (sarmalayıcı tekliği korunur, ızgarada ham `tablo-sar` yok denetimi eklendi).
  - `S1iGorselSigma2Testleri.S1i_13_Kirik_hucre_kilitleri_telefon_yetki_denetim` (`class="hucre-tek-satir"` arıyordu) → `HucreSinif = _ => "hucre-tek-satir"` aranır.
  Düzeltme sonrası filtreli koşum (MobilPariteCss + S1iGorselSigma2 + Grid*): **93/93 yeşil**. Tam süit kuralı gereği ("tam süit BİR kez") yeniden koşulmadı → bu iki düzeltmenin tam süit kanıtı orkestratör kapısındadır. Başka şeride ait kırmızı YOK.

## 8. Değişen dosyalar (bu paket)
```
 M dotnet/Kuyumcu.Api/Modules/Cari/CariModulu.cs        (Listele: sirala/yon)
 M dotnet/Kuyumcu.Web/Pages/Cariler.razor               (yalnız liste → Izgara + Yukle sıralama satırı)
 M dotnet/Kuyumcu.Web/wwwroot/index.html                (izgara.css bağlantısı)
 ?? dotnet/Kuyumcu.Api/Modules/Cari/CariSiralama.cs
 ?? dotnet/Kuyumcu.Api/Modules/Tercih/TercihModulu.cs   (dizin zaten izlenmiyordu; dosya değişti)
 ?? dotnet/Kuyumcu.Core/Domain/IzgaraTercihi.cs
 ?? dotnet/Kuyumcu.Core/I18n/Sozlukler.Izgara.cs
 ?? dotnet/Kuyumcu.Web/Bilesenler/Grid/ (Izgara.razor, IzgaraKolonu.cs, IzgaraDuzeni.cs, IzgaraTercihIstemcisi.cs)
 ?? dotnet/Kuyumcu.Web/wwwroot/css/izgara.css
 ?? dotnet/denetim.sh                                   (yalnız GRID-1 bloğu; dosya zaten izlenmiyordu)
 ?? dotnet/Kuyumcu.Tests/GridTestleri.cs
 M dotnet/Kuyumcu.Tests/MobilPariteTestleri.cs          (B9: ızgaradaki KartListe)
 M dotnet/Kuyumcu.Tests/S1iGorselSigma2Testleri.cs      (S1i_13: HucreSinif)
```
`dotnet/denetim.sh.mutbak` bu pakete ait DEĞİL (başka şeridin artığı; dokunulmadı).
**Dokunulan sayfalar (DoD EK 2):** `Pages/Cariler.razor` (yalnız liste ızgarası).

## 9. Koşul karşılama
| Koşul | Durum | Kanıt |
|---|---|---|
| E-P2-1 kolon sırası fare + klavye, gizle/göster, zorunlu gizlenemez, sunucu sıralaması | KARŞILANDI | canlı Chrome (sürükle, Alt+→, panel, kilit), GE04–GE08/GE13, GH08 |
| E-P2-2 çıkış/giriş sonrası aynı; başka kullanıcı varsayılan; varsayılana dön | KARŞILANDI | canlı A/B, GE09, GH05 |
| E-P2-3 bozuk/eski sürüm → varsayılan, hata yok | KARŞILANDI | canlı (bozuk SQL + s=9), GE03, GD05 |
| E-P2-4 375 px kart düzeni, gerçek tarayıcı görüntüsü | KARŞILANDI | ölçüm tablosu, `olcum-*-375*.png` |
| E-P2-5 ham tablo sayacı 96'dan başlar, yalnız azalır | KARŞILANDI | denetim 95 (tavan 95), M-GRID1-13 |
| Tercih ucu yetki + boyut sınırı testleri | KARŞILANDI | GH03, GH04, GH05, GH07, canlı |
| Gerçek başsız Chrome 1440/1280/375 BASİT+PRO açık+koyu (Read) | KARŞILANDI | § 6 |
| Mutasyon ≥1 kırmızı, cmp AYNI | KARŞILANDI | § 5 (13 mutasyon) |
| build 0/0 | KISMEN | 0 hata; 4 uyarı başka dosyalarda (bu pakete ait değil) |
| denetim TEMİZ (P-30 hariç) | KARŞILANDI | § 7 |
| tam süit 0 yeni kırmızı | KARŞILANDI (düzeltme filtreli kanıtla) | 2 kırmızı bu paketin, düzeltildi, 93/93; tam süit tekrar koşulmadı (§ 7) |

## 10. Açık kalanlar
- Diğer 119 grid (envanter G2) göç paketlerinde (GOC-A..D) ızgaraya taşınacak; her taşınan ekranın adı `IzgaraTercihKurali.BilinenEkranlar`'a eklenir, `GRID1_TAVAN` aşağı çekilir; sayfaya özgü kart CSS'leri silinip `izgara.css` kalıbına bırakılır.
- Diğer liste uçlarına `sirala=&yon=` göç paketiyle eklenecek (§ G1).
- Kurucu/mali müşavir: mevzuat içeren hüküm YOK (arayüz tercihi + sıralama) — soru yok.

---

## 11. TUR 2 (08.10 ~04:30–05:20) — doğrulayıcı bulgularının kapatılması

### 11.1 Bulgu → düzeltme
| Bulgu | Kök neden (koda bakıldı) | Düzeltme | Kanıt |
|---|---|---|---|
| **KRİTİK** BASİT 375 kart: sütun sırası değişince bakiye uzun adın üstüne biniyor | `tuval-panel-musteri.css` ≤768 BASİT kart kuralları `td:first-child` (ad varsayımı) / `td:last-child` (eylem). **Ayrıca canlı ölçümde aynı kusur PRO 375'te de bulundu**: `ortak.css` ≤560 `#cari-liste-sar` PRO kart kuralları da `:first-child`/`:last-child` ile yer veriyordu (telefon başa alınınca ad tam satır olmuyor, TL bakiye ad satırının sağına geçiyordu). | (1) `Izgara.razor` her hücreye `data-izgara-rol` yazar: TANIMDAKİ ilk zorunlu taşınabilir kolon = `birincil`, taşınamaz (eylem) = `sabit` (görüntü sırasından bağımsız). (2) `izgara.css` tek kart kalıbı: `:where(td[data-izgara-rol=birincil]) { order:-1; grid-column:1/-1 }`, `sabit` → `order:1; grid-column:1/-1` (özgüllük 0 korunur). (3) `tuval-panel-musteri.css` (BASİT) ve `ortak.css` (PRO) `/cariler` kart kuralları `td[data-kolon="ad"]` / `td[data-kolon="eylem"]` + `order` ile yeniden yazıldı (yalnız bu 2+2 satır). | Canlı ölçüm § 11.4: kayıtlı özel sıra (doğrulayıcının birebir değeri `{"s":1,"k":["telefon","ad"],"g":["has"],"o":{"a":"ad","y":"azalan"}}`) ile 12 birleşimde K9=0; 375'te 4 birleşimde ad kartın tepesinde ve tam genişlikte, hücre çakışması 0; ayrıca "has başta" sırayla 375 × 4 birleşim K9=0. Testler GE16 (×2), GC01, GC02, GC03. |
| **ORTA** Açılışta liste iki kez isteniyor (önce sırasız) | `Cariler.OnInitializedAsync` → `Yukle()` sıralamasız; ızgara liste dolunca kurulup tercihteki sıralamayı `SiralamaDegisti` ile ikinci kez istiyordu; liste boşalıp dolunca tekrar. | (1) `IzgaraTercihIstemcisi.KayitliSiralamaAsync<T>(ekran, gridId, kolonlar)` — sayfa ilk `Yukle()`'den ÖNCE kayıtlı sıralamayı okur (bozuk/eski/ağ hatası → null = varsayılan sıra). (2) `Izgara` yeni parametresi `GecerliSiralama`: açılışta kayıtlı sıralama sayfanın o anki sıralamasına EŞİTSE `SiralamaDegisti` çağrılmaz (yalnız farkta). (3) Tanım→düzen ve sıralama eşlemesi `IzgaraKolonu<T>.Tanimlar / SiralamaKarsiligi` tek yerde (ızgara ve ön okuma aynı yoldan). | Canlı (fetch kaydı): açılışta istek dizisi `GET /tercihler/benim` → `GET /cari?limit=100&aktif=true&sirala=ad&yon=azalan` (TEK liste isteği, sıralı) → `/kur/guncel` → ızgaranın `/tercihler/benim` okuması. Arama "yokboyle-zzz" (tablo kalktı) + temizle → yalnız 2 liste isteği (aramanın kendisi + temizleme), ek sıralı GET YOK. Testler GE14, GE15. |
| DÜŞÜK K3 kırık (BASİT ad + tür rozeti) | Yeniden üretildi: "Zeynep Ak" + `HER_IKISI` rozeti → BASİT 1280 ve 375'te K3 kırık=1 (`td.hucre-vurgu "Zeynep AkHer İkisi"`). Ekran görüntüleri Read ile incelendi (`t2-varsayilan-heriki-BASIT-aydinlik-1280.png`, `…-karanlik-375.png`): ad ve rozet **tek satırda**, kırılma görünmüyor. Hücre içeriği GRID-1'de değişmedi (CariHucreAd AYNEN taşındı). PRO'da 0. | Kod değişikliği yok — ölçerin K3 sezgisi rozet dolgusunu ikinci satır sayıyor (yalancı pozitif olası). | Açık kalanlara yazıldı (sahibi: ölçer betiği / göç paketi). |
| DÜŞÜK `HAM_TABLO` sayacı Razor yorumundaki `<table`'ı sayıyor | `denetim.sh` GRID-1 bloğu yorum ayıklamıyordu. | Saymadan önce `re.sub(r"@\*.*?\*@", "", m, flags=re.S)`. Sayaç değeri değişmedi (95; mevcut sayfalarda yorum içinde `<table` yok). | Fikstür kanıtı: yalnız `@* … <table class="x"> … *@` içeren bir sayfa ile blok `0 0 1` sayar; satır kaldırılınca (mutasyon) `1 0 1` sayar. |
| DÜŞÜK "tam süit tekrar koşulmadı" | — | Tur 2'de tam süit son kodla BİR kez koşuldu (§ 11.3). | — |

### 11.2 Süreç olayı (md.1 — gizlenmez)
`denetim.sh`'e yorum ayıklama satırı ilk kez `perl -0777 -pi` ile eklenmek istendi; desen dosyada **başka bir şeridin bloğundaki (satır ~5125, tablo sarmalayıcı denetimi) aynı iki satırla** önce eşleşti ve perl, eklenen geniş karakterler yüzünden **bütün dosyayı çift UTF-8 kodlamasıyla** yazdı (`file`: "LF, NEL line terminators"). Hemen fark edildi: bozuk kopya `/private/tmp/kuyumcu-serit/GRID-1/tur2/denetim.sh.bozuk` alındı; dosya byte düzeyinde geri çözüldü (her < U+0100 karakter → tek bayt, eklenen satırın karakterleri UTF-8), yanlış bloğa giren satır silindi, satır yalnız GRID-1 bloğuna eklendi. Doğrulama: `file` → "UTF-8 text" (NEL yok), mojibake deseni (`Ã`, `Ä±`, `Å`) 0, satır sayısı 7128 (bozuk kopyayla aynı → yalnız 1 satır yer değiştirdi), `bash -n` temiz, denetim çıktısı tur 1 ile aynı (yalnız P-30 ✗). Diğer şeridin bloğunun içeriği değişmedi.

### 11.3 Derleme · test · denetim (tur 2, son kod)
- `dotnet build Kuyumcu.sln`: **0 hata**; derlemeli koşumda 4 uyarı, dördü de başka dosyalarda (`SUBEKAPSAMASinirTestleri.cs:49`, `KesifP2Testleri.cs:455`, `SonKalanlarTestleri.cs:584, 813`) — tur 1 ile aynı, bu pakete ait değil.
- Grid filtresi (`FullyQualifiedName~Grid`): **75/75 yeşil** (tur 1: 63 → +12: GE14, GE15, GE16×2, GC01–GC03 ve tur içi ek koşumlar).
- Cariler'i çizen / Cariler CSS'ini okuyan tüm sınıflar (kaynakta `SayfaCariler`, `Pages.Cariler`, `tuval-panel-musteri`, `izgara.css`, `Cariler.razor`, `"/cariler"` geçen 100+ sınıf): **1129/1129 yeşil** (ortak.css değişikliğinden önce koşuldu; ortak.css sonrası tam süit aşağıda).
- **Tam süit (BİR kez, son kod, `--logger trx --results-directory /private/tmp/kuyumcu-serit/trx-pGRID-1-tur2`): 10861 test · 10859 geçti · 0 başarısız · 2 atlandı** (`TOPLU1SinirTestleriTur2.T2_Kasa_adi_kesme_isaretli`, `SATIS3EkranTestleri.DOG_T24_…` — önceden de atlanan). Yeni kırmızı 0.
- `denetim.sh` (bash, son kod): tek ✗ **P-30 DEMO_INSAN_SURESI** (kurucu kalemi, kapsam dışı — "P-30 hariç TEMİZ"); `✓ GRID-1: HAM_TABLO_ORTAK_GRID_DISI 95 (tavan 95, yalnız azalır; yazdırma belgesi 4)`.

### 11.4 Mutasyon tablosu (tur 2; yedek `/private/tmp/kuyumcu-serit/mut-yedek/GRID-1-tur2/` → TEK eşleşmeli değişiklik → filtreli test → `shutil.copyfile` ile geri (yeni mtime) → `filecmp` birebir)
| Kod | Değişiklik | Kırmızı olan | cmp |
|---|---|---|---|
| M-GRID1-14 | `tuval-panel-musteri.css` BASİT kart: `td[data-kolon="ad"]` → `td:first-child` | GC02, GC03 | AYNI |
| M-GRID1-15 | `izgara.css` birincil hücreden `order:-1` kaldırıldı | GC01 | AYNI |
| M-GRID1-16 | `Izgara.KartRolu` birincil rolü hiç vermiyor | GE16 (PROFESYONEL), GE16 (BASIT) | AYNI |
| M-GRID1-17 | `Cariler` ön okuma sonucu atılıyor (ilk istek sırasız) | GE14 | AYNI |
| M-GRID1-18 | `Izgara` açılışta `GecerliSiralama` karşılaştırması yerine her kayıtlı sıralamada yeniden istek | GE14 | AYNI |
| M-GRID1-19 | `ortak.css` PRO kart: `td[data-kolon="ad"]` → `td:first-child` | GC03 | AYNI |
| M-GRID1-20 | `denetim.sh` GRID-1 yorum ayıklama satırı yok (fikstür kopyası üzerinde) | sayaç `0 0 1` → `1 0 1` (yorumdaki tablo sayıldı) | (gerçek dosyaya dokunulmadı) |
Son mutasyondan sonra temiz derleme + tam süit (§ 11.3).

### 11.5 Canlı doğrulama (API 5160, vekil 5161, CDP 9700; test kiracısı `PAKETGRID-1-TEST tur2 …` = 01M4CK40NW6Z923WS9J38K8N87; A=SAHIP, B=MUDUR; 6 cari: uzun ad "Ahmet Yılmaz Kuyumculuk Ticaret Limited Şirketi", "Zeynep Ak" HER_IKISI, Beta 500 TL / Gama 50 TL veresiye)
**API (gövdeler okundu):**
| Adım | Beklenen | Gerçek |
|---|---|---|
| A PATCH doğrulayıcının değeri | 200 | 200; A GET aynı değer: True |
| B GET | A'nın anahtarı görünmez | görünür mü: False |
| B PATCH bozuk JSON / yinelenen kolon / 2041 karakter | 400 ×3 | 400 "…geçerli JSON değil." · 400 "…'k' içinde yinelenen kolon: ad." · 400 "…çok uzun: en çok 2000 karakter." |
| B DELETE aynı anahtar (kendi kaydı yok) | 200, A'nınki durur | 200; A'nınki duruyor: True |
| `GET /cari?sirala=ad&yon=azalan` | Z→A | Zeynep Kara, GRIDSIRA Gama, Beta, Alfa, Ahmet… |
| `GET /cari?sirala=has` | 400 | 400 "Bilinmeyen sıralama alanı: has. Geçerli: ad, tur, telefon, tl-bakiye." |
SQL: `kullanici_tercihleri` bu kiracıda yalnız A'nın satırı (`grid:cariler:cari-liste | grid1-a-…@test.local | {"s":1,"k":["telefon","ad"],"g":["has"],"o":{"a":"ad","y":"azalan"}}`); bozma sonrası `{bozuk-eski-kayit`; "Varsayılana dön" sonrası satır 0. `denetim_kayitlari` (varlik_turu KullaniciTercihi): EKLEME "Arayüz tercihi güncellendi: grid:cariler:cari-liste" ×6, EKLEME ×2, GUNCELLEME ×1, SILME ×2.

**Başsız Chrome senaryoları:**
| Senaryo | Sonuç |
|---|---|
| Açılış istek dizisi (fetch kaydı) | tercih → TEK sıralı liste GET; aria-sort ad=descending |
| Boşalt (arama sonuçsuz, tablo kalktı) + doldur | yeni liste istekleri yalnız 2 (arama + temizleme), ek GET yok; sıra korunuyor |
| A çıkış (jetonlar silindi, /giris) + yeniden giriş | önce/sonra kolonlar `telefon·tl-bakiye·ad·tur·eylem`, satırlar Z→A, ad=descending — AYNI |
| B açılış | varsayılan `ad·tur·telefon·tl-bakiye·has·eylem`, sıra varsayılan; "Varsayılana dön" kilitli + "Düzen zaten varsayılan" |
| A kaydı SQL ile bozuldu | varsayılan düzen; ızgara hatası yok, hata bandı 0, blazor hatası yok; "Varsayılana dön" → "Varsayılan sütun düzenine dönüldü." |

**Ölçüm (`sikisma-olc.js`, panel kapalı ve açık):**
| Tercih | Mod | Tema | Genişlik | Kolonlar | tr | K1 | K3 taşan/kırık | K5 TRY | K9 binen | K11 | yatay | K2 | 375 kart: ad tepede / tam genişlik / hücre çakışması | panel K3/K9/yatay |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| telefon başta (doğrulayıcı) | PRO | açık | 1440 | telefon·tl-bakiye·ad·tur·eylem | table-row | 0 | 0/0 | 0 | 0 | 0 | 0 | 98 | — | 0/0/0 |
| 〃 | PRO | açık | 1280 | 〃 | table-row | 0 | 0/0 | 0 | 0 | 0 | 0 | 98 | — | 0/0/0 |
| 〃 | PRO | açık | 375 | 〃 | grid | 0 | 0/0 | 0 | 0 | 0 | 0 | 97 | evet / evet / 0 | 0/0/0 |
| 〃 | PRO | koyu | 1440 | 〃 | table-row | 0 | 0/0 | 0 | 0 | 0 | 0 | 98 | — | 0/0/0 |
| 〃 | PRO | koyu | 1280 | 〃 | table-row | 0 | 0/0 | 0 | 0 | 0 | 0 | 98 | — | 0/0/0 |
| 〃 | PRO | koyu | 375 | 〃 | grid | 0 | 0/0 | 0 | 0 | 0 | 0 | 97 | evet / evet / 0 | 0/0/0 |
| 〃 | BASİT | açık | 1440 | telefon·tl-bakiye·ad·eylem | table-row | 0 | 0/0 | 0 | 0 | 0 | 0 | 98 | — | 0/0/0 |
| 〃 | BASİT | açık | 1280 | 〃 | table-row | 0 | 0/0 | 0 | 0 | 0 | 0 | 98 | — | 0/0/0 |
| 〃 | BASİT | açık | 375 | 〃 | grid | 0 | 0/0 | 0 | **0** | 0 | 0 | 97 | evet / evet / 0 | 0/0/0 |
| 〃 | BASİT | koyu | 1440 | 〃 | table-row | 0 | 0/0 | 0 | 0 | 0 | 0 | 98 | — | 0/0/0 |
| 〃 | BASİT | koyu | 1280 | 〃 | table-row | 0 | 0/0 | 0 | 0 | 0 | 0 | 98 | — | 0/0/0 |
| 〃 | BASİT | koyu | 375 | 〃 | grid | 0 | 0/0 | 0 | **0** | 0 | 0 | 97 | evet / evet / 0 | 0/0/0 |
| has başta | PRO | açık/koyu | 375 | has·tl-bakiye·telefon·ad·tur·eylem | grid | 0 | 0/0 | 0 | 0 | 0 | 0 | 97 | evet / evet / 0 | 0/0/0 |
| has başta | BASİT | açık/koyu | 375 | has·tl-bakiye·telefon·ad·eylem | grid | 0 | 0/0 | 0 | 0 | 0 | 0 | 97 | evet / evet / 0 | 0/0/0 |
| varsayılan (+HER_IKISI cari) | PRO | açık/koyu | 1280, 375 | ad·tur·telefon·tl-bakiye·has·eylem | — | 0 | 0/0 | 0 | 0 | 0 | 0 | 97–98 | — | 0/0/0 |
| varsayılan (+HER_IKISI cari) | BASİT | açık/koyu | 1280, 375 | ad·telefon·tl-bakiye·has·eylem | — | 0 | 0/**1** | 0 | 0 | 0 | 0 | 97–98 | — | 0/0–1/0 |
İlk "telefon başta" ölçümü (ortak.css düzeltmesinden ÖNCE) PRO 375'te `ad tam genişlik = HAYIR` verdi ve görüntüde TL bakiye ad satırının sağındaydı → ortak.css düzeltildi, yeniden ölçüldü (yukarıdaki tablo). Ölçer betiğinin `kartGeo` alanında BASİT telefon hücresi için anahtar boş yazıldı; doğrudan DOM sorgusunda `data-kolon="telefon"` VAR (ölçüm betiği yan etkisi, ürün değil).
Görüntüler (Read ile görüldü): `/private/tmp/kuyumcu-serit/GRID-1/tur2/tarayici/foto/` — `t2-telefon-basta-<MOD>-<tema>-<w>[-panel].png` (12+12), `t2-has-basta-*-375*.png`, `t2-varsayilan-heriki-*`, `istek-acilis-PRO-1440.png`, `A-yeniden-giris-PRO-1440.png`, `B-varsayilan-PRO-1440.png`, `A-bozuk-tercih-PRO-1440.png`. Bakılanlar: BASİT açık 375 (telefon başta: ad üstte tam satır, altında telefon + sağda Bakiye, eylemler altta, binme yok), PRO koyu 375 (ilk ölçüm kusuru → düzeltme), BASİT koyu 375 has başta, BASİT 1280/375 HER_IKISI (rozet adla tek satırda).

**Temizlik:** API (PID) ve vekil durduruldu; 5160/5161/9700 dinleyen yok. `zsh kiraci-temizle.sh 01M4CK40NW6Z923WS9J38K8N87 --uygula` → 75 tablodan 359 satır + tenants 1 satır; kiracı satırı 0; davet satırı (yalnız `olusturan_not='PAKET GRID-1 canli test'`) silindi → 0. `tenants`: başlangıç 16 → bitiş 17; fark benim değil: koşum sırasında GIRDI-AILESI-2 şeridi `PAKETGIRDI-AILESI-2-TEST T2` (01M4CKM2G8QYMYWJFGB8CVW32K, 08.10 01:56 UTC) kiracısını açtı — dokunulmadı. Bu paketin kiracısı 0.

### 11.6 Tur 2 değişen dosyalar
```
M  dotnet/Kuyumcu.Web/Bilesenler/Grid/Izgara.razor             (data-izgara-rol · GecerliSiralama · ortak eşleme)
M  dotnet/Kuyumcu.Web/Bilesenler/Grid/IzgaraKolonu.cs          (Tanimlar / SiralamaKarsiligi — tek kaynak)
M  dotnet/Kuyumcu.Web/Bilesenler/Grid/IzgaraTercihIstemcisi.cs (KayitliSiralamaAsync)
M  dotnet/Kuyumcu.Web/Pages/Cariler.razor                      (ön okuma + GecerliSiralama; aynı paket, yalnız liste ızgarası)
M  dotnet/Kuyumcu.Web/wwwroot/css/izgara.css                   (rol kuralları)
M  dotnet/Kuyumcu.Web/wwwroot/css/tuval-panel-musteri.css      (BASİT kart: 2 satır :first/:last-child → data-kolon)
M  dotnet/Kuyumcu.Web/wwwroot/css/ortak.css                    (PRO kart: 2 satır :first/:last-child → data-kolon)
M  dotnet/denetim.sh                                           (yalnız GRID-1 bloğu: yorum ayıklama)
M  dotnet/Kuyumcu.Tests/GridTestleri.cs                        (GE14, GE15, GE16, GridKartCssTestleri GC01–GC03)
```
**Sapma (DOSYA ALANI):** `tuval-panel-musteri.css` ve `ortak.css` alan listesinde yoktu; doğrulayıcının (a) seçeneği BASİT sayfa kuralının değiştirilmesini açıkça istedi, PRO'daki aynı kusur canlıda bulundu. Değişiklik yalnız `/cariler` kart kuralının 2+2 satırıdır (sıra seçicisi → sütun anahtarı); başka sayfa etkilenmez (`#cari-liste-sar` ve `.musteri-tablo` yalnız Cariler'de). **Dokunulan sayfalar (DoD EK 2):** `Pages/Cariler.razor` — aynı paketin 2. turu (doğrulayıcı ORTA bulgusu gerekçesiyle).

### 11.7 Koşul karşılama (tur 2 güncel)
| Koşul | Durum | Kanıt |
|---|---|---|
| E-P2-1 sıra/gizle/sunucu sıralaması | KARŞILANDI | tur 1 + § 11.5 |
| E-P2-2 A çıkış/giriş aynı, B varsayılan, varsayılana dön | KARŞILANDI | § 11.5 canlı |
| E-P2-3 bozuk/eski → varsayılan, hata yok | KARŞILANDI | § 11.5 canlı (bozuk), GE03 |
| E-P2-4 375 kart (özel sırayla da) | KARŞILANDI | § 11.5 ölçüm, K9=0 her birleşimde; GE16, GC01–GC03 |
| E-P2-5 ham tablo sayacı | KARŞILANDI | 95/95, yorumlar sayılmıyor |
| Tek liste isteği (doğrulayıcı ORTA) | KARŞILANDI | GE14/GE15 + canlı fetch kaydı |
| Tercih ucu yetki + boyut | KARŞILANDI | GH03–GH07 + canlı |
| Mutasyon ≥1 kırmızı, cmp AYNI | KARŞILANDI | § 5 (13) + § 11.4 (7) |
| build 0 hata | KARŞILANDI (uyarılar başka dosyalarda) | § 11.3 |
| denetim TEMİZ (P-30 hariç) | KARŞILANDI | § 11.3 |
| Tam süit 0 yeni kırmızı | KARŞILANDI | 10861 / 0 başarısız |

### 11.8 Açık kalanlar (tur 2)
- K3 "kırık" BASİT ad hücresi + tür rozeti (HER_IKISI/TEDARIKCI rozetli kısa ad): görüntüde tek satır; ölçer sezgisi (sahibi: `betikler/sikisma-olc.js` K3) ya da göç paketinde ad hücresi rozet düzeni gözden geçirilsin.
- `olcek.css:326` ve `tuval-panel-musteri.css:271` masaüstü tablo kuralları ad hücresinin kalınlığını hâlâ `td:first-child` ile verir (kural gereği kullanıcı telefonu başa alırsa masaüstünde ilk hücre olan telefon kalınlık alır — bu turda tarayıcıda ayrıca ölçülmedi; ölçümde K3/K9/yatay 0). Yerleşim/binme etkisi YOK; `OlcekTekTestleri` bu seçiciyi birebir aradığından bu turda değiştirilmedi → göç paketi (GOC-A) `olcek.css` sahibiyle birlikte `[data-kolon]` seçicisine geçirsin.
- Diğer 119 ızgara göçü ve diğer liste uçlarına `sirala=&yon=` (tur 1'deki gibi).

