# PAKET P-37b — KARŞILAMA EKRANI: hoş geldin + kullanıcı adı + hava + kutucuk düzeni

> Dalga 6 · Taşkın (backend) + Ertuğ (frontend) · 2026-09-21
> Önkoşul: P-37a (menü/araç şeridi düzen disiplini) — BİTTİ, bu pakette TEKRARLANMADI.

---

## 0. DURUM BEYANI — **BİTTİ**

Kurucunun üç kalemi (kutucuklu karşılama · selam+ad+tarih · hava) ve "ekrana
kilitleyen tasarım"ın **dört ölçülebilir kabul şartı** uygulandı, **canlı
ölçümle** kanıtlandı, üç cırcır sayacı denetim.sh'a eklendi, tam süit yeşil.

Bir şey **uydurulmadı**: hava kutucuğunun gerçek Open-Meteo yanıtı doğrulama
tarayıcısında alınamadı (tarayıcı dış ağa çıkmıyor; ana makine aynı adrese
`200` alıyor). Hava kutucuğunun **çizim yolu** canlıda, `fetch` vekiliyle
verilen **açıkça sahte** bir yanıtla doğrulandı; **düşme yolu** ise gerçek
ağ hatasıyla doğrulandı. İkisi de aşağıda ayrı ayrı yazılıdır (§ 7.4).

---

## 1. Sapmalar ve gerekçeleri

| # | Spesifikasyon / beklenti | Gerçek | Karar |
|---|---|---|---|
| S1 | "Panel.razor bugün 351 satır; 'hava', 'Hoş geldin' geçmiyor" | Dosya bu paketin **yarıda kalmış ilk koşumunda** 542 satıra çıkmış, hava + selam + kutucuk altyapısı yazılmıştı; 20 bUnit testi de vardı ve yeşildi | Sıfırdan yazılmadı; yarım iş **denetlendi, eksikleri tamamlandı** (sayaç, CSP kapısı, i18n, ölçüt-1 düzeltmesi, canlı doğrulama, rapor) |
| S2 | `KARSILAMA_TERCIH_KULLANICI_DISI` sayacı yok | denetim.sh'ta 4 P-37b sayacı vardı, bu **yoktu** | Eklendi (§ 5) |
| S3 | Hava kaynağı seçilecek | Kaynak `Open-Meteo`, **CC BY 4.0, ücretsiz katman ticari olmayan kullanım için**; ticari kullanım ücretli plan ister | Varsayılan **KAPALI**; kaynak/lisans **kurucu kalemi** (§ 9) |
| S4 | — (spesifikasyonda yok) | **CANLI BULGU:** `Content-Security-Policy: connect-src 'self'` hava isteğini tarayıcıda **her zaman** kesiyordu → kutucuk kodda çalışsa bile asla veri alamazdı | Fail-closed CSP kapısı eklendi (`Hava:KaynakOrigin`), varsayılan davranış **değişmedi** (§ 3.2) |
| S5 | — (spesifikasyonda yok) | **CANLI BULGU:** 1440×900'de hızlı eylemler satırı ilk ekrana sığmıyordu (alt kenar **963 px**) | Kutucuk içi stat ızgarası geniş ekranda 2 sütun → **823 px** (§ 3.3) |
| S6 | "Sozlukler TR+EN, çıplak metin yok" | WMO durum metni `HavaServisi` içinde **yalnız Türkçe** sabitti | `Kuyumcu.Core/I18n/HavaSozlugu.cs` (TR+EN) açıldı; `HavaDurumu` artık **ham kod** taşır, çeviri ekranda seçili dille yapılır (§ 3.4) |

Mevzuat/yasal hüküm içeren bir kalem **yok** (karşılama ekranı bilgi ekranıdır).

---

## 2. Migration

**GEREKMEDİ.** Şema değişikliği yok. Kutucuk düzeni, P-37a'da kurulan
`kullanici_tercihleri` tablosunu (`20260921084754_P37KullaniciTercihi`) ve
`api/v1/tercihler/benim` ucunu kullanır; **yeni uç açılmadı**.
`kuyumcu_net` üzerinde canlı kanıt § 7.3'te.

---

## 3. Yapılanlar

### 3.1 Karşılama ekranı — `Kuyumcu.Web/Pages/Panel.razor`
* `/` artık **pencere pencere** yedi kutucuk: `gun-ozeti · kur · bekleyen ·
  kasa · hizli-eylem · son-islemler · grafik`.
* Üstte **saate göre selam + kullanıcı adı + tarih** (`#panel-selam`,
  `#panel-tarih`), sağ üstte **hava** (`#panel-hava`).
* Kutucuklar **sürükle-bırakla** dizilir (`siralistesi.js`, `panel-izgara`),
  **Alt+↑/↓** klavye yolu vardır, **gizlenir/geri getirilir**.
* Düzen **KULLANICI** bazında saklanır: `MenuTercihServisi` →
  `PATCH/DELETE /api/v1/tercihler/benim`, anahtar `panel-duzeni`.
* Her kutucuk **kendi isteğini** açar, kendi **iskeletini** ve kendi **hata
  bandını** taşır; sayfayı tümden kilitleyen `<Yukleniyor />` kapısı **yok**.

### 3.2 CSP kapısı — `Kuyumcu.Api/Common/HavaOrigin.cs` (YENİ) + `Program.cs`
`connect-src 'self'` dış hava isteğini kesiyordu. Artık:
`connect-src 'self'" + havaCspEki` — `havaCspEki`, `Hava:KaynakOrigin`
yapılandırmasından **yalnız**: mutlak, **https**, yolsuz/sorgusuz, boşluk ve
`;` `,` `'` `"` içermeyen bir değerin **authority** parçası olarak üretilir.
Boş/şüpheli her değerde **boş dizge** döner → **varsayılan davranış birebir
eskisi** (hiçbir dış origin açılmaz). Başlık enjeksiyonu testlerle kapalı.

### 3.3 Ölçüt 1 düzeltmesi — `Kuyumcu.Web/wwwroot/css/app.css`
```css
@media (min-width: 1200px) {
  .karsilama-kutucuk .stat-izgara { grid-template-columns: repeat(2, minmax(0, 1fr)); }
}
```
Sabit renk yok, satır içi style yok, yalnız belirteç/oran.

### 3.4 Dil — `Kuyumcu.Core/I18n/HavaSozlugu.cs` (YENİ)
WMO kodu → TR/EN metin **tek kaynakta**; `HavaDurumu` kaydı artık `int? Kod`
taşır (metin değil). Ham kod ekrana **basılmaz** (UX-S31).
`Sozluk.Panel` TR+EN anahtarları (selam, kutucuk başlıkları, düzen eylemleri)
zaten mevcut ve iki dilde dolu.

### 3.5 Hava servisi — `Kuyumcu.Web/Servisler/HavaServisi.cs`
`Hava:Etkin` **varsayılan false** · `HttpClient.Timeout = 3 sn` ·
`CancellationTokenSource(3 sn)` · **her istisna yutulur, `null` dönülür** ·
15 dk bellek önbelleği · **konum izni İSTENMEZ** (şehir yapılandırmadan;
`Permissions-Policy: geolocation=()` zaten kapalı).

---

## 4. Derleme

```
dotnet build Kuyumcu.sln --nologo  →  rc=0 · 0 Hata · 0 Uyarı
```

## 5. denetim.sh

```
./denetim.sh → rc=0
  ✓ P-37b: JS_SOZDIZIM_HATASI                      0
  ✓ P-37b: HAVA_BEKLETME_KAPISI                    1 (zaman aşımı + iptal + sessiz düşme + varsayılan KAPALI)
  ✓ P-37b: KARSILAMA_TEK_ISTEK_BEKLEME             0 (paralel yükleme, 6 iskelet)
  ✓ P-37b: KARSILAMA_HAVA_KONUM_IZNI               0
  ✓ P-37b: KARSILAMA_TERCIH_KULLANICI_DISI         0 (düzen kullanıcı bazlı: istemci 0 · sunucu 0 sızıntı)
DENETİM TEMİZ.
```
`KARSILAMA_TERCIH_KULLANICI_DISI` **iki tarafı birden** ölçer: istemcide
`/tercihler/` çağrılarından `benim` içermeyen **0**; sunucuda
`kullanici_tercihleri` sorgularından `KullaniciId` süzmeyen **0**.
denetim.sh, düzenlemeden önce scratchpad'e tarih damgalı yedeklendi
(P-37a'nın 0-bayt olayının dersi) ve **python3 ile** düzenlendi.

## 6. Testler

| Koşum | Kapsam | Toplam | Başarılı | Başarısız | Atlanan |
|---|---|---|---|---|---|
| Filtreli | `P37bKarsilamaTestleri` | **31** | 31 | 0 | 0 |
| Tam süit (öncesi, P-37a kapanışı) | `Kuyumcu.Tests` | 3651 | 3651 | 0 | 0 |
| **Tam süit (sonrası)** | `Kuyumcu.Tests` | **3682** | **3682** | **0** | 0 |

**Yeni test sayısı: 31** (yarım koşumdan devralınan 20 + bu koşumda yazılan 11).
trx: `scratchpad/trx-pP-37b-final`.

## 7. Mutasyon kanıtı

| Kod | Dosya | Değişiklik | Kırmızıya dönen test | Geri alma |
|---|---|---|---|---|
| M37b-1 | HavaServisi.cs | `Timeout` 3 sn → 5 dk | `Hava_BEKLETME_KAPISI_kodda_vardir` | CMP-AYNI |
| M37b-2 | HavaServisi.cs | `catch (Exception)` → `catch (JsonException)` | `Hava_kaynak_cevap_vermezse_SESSIZCE_null_doner` | CMP-AYNI |
| M37b-3 | Panel.razor | selamdan kullanıcı adı çıkarıldı | `Karsilama_selami_KULLANICI_ADINI_tasir` | CMP-AYNI |
| M37b-4 | Panel.razor | defter hatası → sessizce boş liste (UX-S28 ihlali) | `Karsilama_son_islemler_HATASI_bos_duruma_cevrilmez` | CMP-AYNI |
| M37b-5 | Panel.razor | `GizleDegistir` → `Task.CompletedTask` | `Kutucuk_gizlenince_KULLANICI_tercihine_PATCH_atilir` | CMP-AYNI |
| M37b-6 | PanelKutucuklari.cs | `Kasa` kutucuğu katalogdan silindi | `Karsilama_sabahin_BES_SORUSUNU_kutucuk_olarak_tasir` **+** `Karsilama_bir_UC_PATLASA_bile_diger_kutucuklar_cizilir` | CMP-AYNI |
| M37b-7 | app.css | ölçüt-1 medya kuralı silindi | `Karsilama_genis_ekranda_kutucuk_ici_stat_izgarasi_IKI_SUTUNDUR` | CMP-AYNI |
| M37b-8 | Program.cs | CSP'ye ham yapılandırma değeri kondu (kapı atlandı) | `Program_cs_connect_src_i_hava_kapisindan_gecirir` | CMP-AYNI |
| M37b-9 | HavaOrigin.cs | https şart kaldırıldı | `Hava_CSP_eki_supheli_degerde_KAPALI_kalir` | CMP-AYNI |
| M37b-10 | HavaSozlugu.cs | dil ayrımı kaldırıldı (hep TR) | `Hava_ham_WMO_kodu_ekrana_basilmaz_sozlukten_cevrilir` | CMP-AYNI |

Test açığı bulunmadı; hiçbir mutasyon "kimseyi kırmadan" geçmedi.
Son mutasyondan sonra **temiz `dotnet build` (0/0)** ve **tam süit yeşil**.

> ⚠ **Olay kaydı (md.1):** M37b-8'in geri alınması sırasında `Program.cs`
> yedeği CSP yamasından **önce** alınmış olduğu için geri kopyalama yamayı da
> sildi; tam süitte `Program_cs_connect_src_i_hava_kapisindan_gecirir` **1
> başarısız** verdi (3681/3682). Gizlenmedi: yama yeniden uygulandı, yedek
> tazelendi, süit yeniden koşuldu → **3682/3682**.

---

## 7.x CANLI DOĞRULAMA (localhost:5198 — kurucunun 5199'una dokunulmadı)

Kurulum: `PAKET37B-TEST` ve `PAKET37B-TEST2` geçici kiracıları sistem daveti
ile açıldı, iş bitince **ikisi de silindi**. Parola/jeton hiçbir çıktıya
yazılmadı; oturum tarayıcıya **parola alanı doldurulmadan**, jeton yerel bir
aktarıcıdan `window.name` ile taşınarak kuruldu.

### 7.1 Dört kabul şartı — ÖLÇÜM

| # | Şart | Ölçüm (1440×900) | Sonuç |
|---|---|---|---|
| 1 | Sabahın beş sorusu **kaydırmadan** görünür | `getBoundingClientRect().bottom`: gun-ozeti **542** · kur **542** · bekleyen **542** · kasa **542** · hizli-eylem **823** · son-islemler **823** — hepsi ≤ 900; `scrollY=0`, yatay taşma **0** | ✅ |
| 2 | Kutucuklar **birbirini beklemez** | Aynı yüklemede 4 ayrı panel isteği **602 / 603 / 606 / 607 ms**'de açıldı (≤5 ms aralık, sıraya girmiyor); toplam 14 API isteği; her kutucuğun kendi iskeleti var | ✅ |
| 3 | Kur **kendiliğinden** tazelenir (yeni altyapı yok) | `/api/v1/kur/guncel` **553 → 10842 → 20843 ms** (~10 sn, mevcut `CanliKurServisi`); ekrandaki değer 6.809,77 → 6.813,32 → 6.810,63 olarak kendiliğinden değişti | ✅ |
| 4 | En sık üç işlem **tek tıkla** | `#panel-satis-yap`→`/islemler` · `#panel-alis-yap`→`/islemler` · `#panel-tahsilat`→`/vezne` (+`#panel-hurda`→`/hurda`), dördü de ilk ekranda | ✅ |

**Ölçüt 1, ilk ölçümde DÜŞTÜ** (hizli-eylem alt kenarı 963 px > 900).
Uydurulmadı: CSS düzeltildi (§ 3.3), test + mutasyonla kilitlendi, ölçüm
tekrarlandı → **823 px**.

### 7.2 Ekran görüntüsü
Oturumda üç ekran görüntüsü alındı (düzeltme öncesi 963 px hâli, düzeltme
sonrası hâl, i18n sonrası son hâl). Son hâlde görünen: **"İyi akşamlar Ayse"**,
**"21 Eylül 2026, Pazartesi"**, sağ üstte **"İstanbul 20°C Yağmurlu"**,
altında yedi kutucuk ve hızlı eylemler — hepsi ilk ekranda.

### 7.3 Kutucuk düzeni — HTTP + SQL kanıtı

| Adım | Beklenen | Gerçek |
|---|---|---|
| "Kutucukları düzenle" → `#panel-gizle-kur` | `PATCH /api/v1/tercihler/benim`, kutucuk kaybolur, "göster" düğmesi doğar | 2 istek `/api/v1/tercihler/benim`; `#panel-kutucuk-kur` **yok**, `#panel-goster-kur` **var** |
| Veritabanı | satır **kullanıcıya** yazılır | `kullanici_tercihleri`: `tenant_id=01M32JP3…, kullanici_id=01M32JP3R9X6C3DQHGYBT1B6N3, anahtar=panel-duzeni, deger={"sira":[],"gizli":["kur"]}` |
| Denetim kaydı | `DenetimServisi.YazmaKaydet` çağrılır | `denetim_kayitlari`: `KullaniciTercihi / EKLEME / "Arayüz tercihi güncellendi: panel-duzeni"` |
| "Düzeni sıfırla" | `DELETE /tercihler/benim/{anahtar}`, kutucuk geri gelir | `#panel-kutucuk-kur` **geri geldi**, hata bandı **yok**, `SELECT COUNT(*) … = 0` |

### 7.4 Hava — iki yol da ayrı ayrı doğrulandı

| Yol | Nasıl | Gerçek |
|---|---|---|
| **Düşme yolu (gerçek)** | Doğrulama tarayıcısı dış ağa çıkamıyor (`fetch(open-meteo) → TypeError: Failed to fetch`; aynı adrese ana makineden `curl` **200**) | Kutucuk **sessizce yok**, **hata bandı yok**, sayfa **beklemedi**, diğer kutucuklar doldu — kurucunun şartı birebir |
| **Çizim yolu (sahte yanıt)** | Sayfada `window.fetch` vekiliyle Open-Meteo'ya **açıkça sahte** `{"current":{"temperature_2m":19.7,"weather_code":61}}` döndürüldü | `#panel-hava-sehir=İstanbul`, `#panel-hava-derece=20°C`, `#panel-hava-durum=Yağmurlu`; **EN**'e geçilince `Rainy` ve `Good evening Ayse` |
| **CSP** | `Hava__KaynakOrigin=https://api.open-meteo.com` ile başlatıldı | Başlık: `connect-src 'self' https://api.open-meteo.com`; değişken boşken başlık **eski hâliyle aynı** |

⚠ **Gerçek Open-Meteo yanıtı bu ortamda ekrana alınamadı.** Sebep ağ kısıtıdır,
kod değil; yine de "gerçek kaynaktan gelen derece canlıda görüldü" **denmiyor**.

### 7.5 Temizlik
`PAKET37B-TEST` ve `PAKET37B-TEST2` kiracıları `tenant_id` kolonu olan **tüm**
tablolardan ve `tenants`ten silindi, kullandığım 2 davet satırı silindi.
`SELECT COUNT(*) FROM tenants` → **9** (başlangıçtaki değer).
`QA-TEST-*` kiracılarına **dokunulmadı**. API ve aktarıcı durduruldu:
`lsof -nP -iTCP:5198 -sTCP:LISTEN` ve `:5197` **boş**. 5199'a hiç dokunulmadı.
Geçici sır dosyaları `printf '' >` ile boşaltıldı.

---

## 8. DoD SÖZLEŞMESİ

| Varlık / kayıt | Düzenleme yolu | Silme / pasife alma | Yetki | Denetim |
|---|---|---|---|---|
| `KullaniciTercihi` (`panel-duzeni`) | `PATCH /api/v1/tercihler/benim` — **gönderilmeyen anahtar korunur, `null` siler** | `DELETE /tercihler/benim/{anahtar}` = "Düzeni sıfırla" (geri dönüş yolu: kutucuklar varsayılan sıraya döner) | `[HerkeseAcik]` + **kendi** kullanıcısı (`TenantId`+`KullaniciId` süzgeci); başka kullanıcının tercihi dönmez/yazılmaz | `DenetimServisi.YazmaKaydet("KullaniciTercihi", …)` — **çağrılır**, yeniden yazılmaz |
| `HavaDurumu` | — | — | — | **Salt okunur, kalıcı değil:** dış kaynaktan gelir, **veritabanına yazılmaz**, 15 dk bellekte durur. Düzenlenecek bir kayıt yoktur (`DegismezIpucu` gerekçesi: kullanıcı verisi değil, gösterim) |

Bu pakette **finansal varlık oluşturulmadı**; hard DELETE yalnız arayüz
tercihi üzerindedir (iş verisi değildir, defteri bozmaz).

**Her düğme ya istek atar ya devre dışıdır:**
`#panel-duzen-modu` (yerel durum) · `#panel-duzen-sifirla` (DELETE) ·
`#panel-gizle-*` / `#panel-goster-*` (PATCH) · hızlı eylemler `<a href>`.
Tıklanınca hiçbir şey olmayan düğme yok. Yeni `ModalPencere` düzenleme kalıbı
**açılmadı**; ikinci "duzenlenenId" kalıbı **yok**. Yıkıcı eylem yok
(sıfırlama geri dönüşlüdür: kutucuklar varsayılana döner, veri kaybı olmaz).

**Dokunulan sayfalar (tek dokunuş kaydı):** `Kuyumcu.Web/Pages/Panel.razor`.

---

## 9. Koşul karşılama tablosu

| Koşul | Durum | Kanıt |
|---|---|---|
| `/` kutucuklu karşılama (özet·kur·bekleyen·son işlemler·hızlı eylem) | ✅ | § 3.1, § 7.1, ekran görüntüsü |
| Kutucuklar sürükle-bırak + gizlenebilir, sıra **kullanıcı** bazlı | ✅ | § 7.3 SQL + `KARSILAMA_TERCIH_KULLANICI_DISI = 0` |
| Mevcut `KullaniciTercihi` / `/tercihler/benim` kullanıldı, yeni uç açılmadı | ✅ | § 2 |
| Saate göre selam + kullanıcı adı + tarih | ✅ | "İyi akşamlar Ayse" / "Good evening Ayse" |
| Hava: şehir işletme ayarından, **konum izni istenmez** | ✅ | `KARSILAMA_HAVA_KONUM_IZNI = 0`, `Permissions-Policy: geolocation=()` |
| Hava: kaynak yanıt vermezse **sessizce** kaybolur, hata bandı yok, sayfa beklemez | ✅ | § 7.4 (gerçek ağ hatasıyla) |
| Hava kaynağının adı + lisans durumu raporda | ✅ | § 10 |
| Ölçüt 1 — beş soru kaydırmadan | ✅ | § 7.1 (963 → 823 px düzeltmesiyle) |
| Ölçüt 2 — kutucuklar birbirini beklemez | ✅ | § 7.1 (602/603/606/607 ms) |
| Ölçüt 3 — kur kendiliğinden tazelenir, yeni altyapı yok | ✅ | § 7.1 (~10 sn) |
| Ölçüt 4 — en sık üç işlem tek tık | ✅ | § 7.1 |
| `HAVA_BEKLETME_KAPISI = 1` · `KARSILAMA_TEK_ISTEK_BEKLEME = 0` · `KARSILAMA_TERCIH_KULLANICI_DISI = 0` | ✅ | § 5 |
| Her kural için bUnit testi + mutasyon kanıtı | ✅ | § 6, § 7 |
| Canlı doğrulama zorunlu (P-37'de yapılmamıştı) | ✅ | § 7.x |
| denetim.sh TEMİZ · tam süit 0 başarısız · test kiracısı silindi | ✅ | § 5, § 6, § 7.5 |
| Pages içinde yeni istemci record açılmadı | ✅ | Ortak tipler `Kuyumcu.Core/I18n` ve `Servisler` altında |
| Yeni CSS'te sabit renk yok, satır içi style yok | ✅ | § 3.3 |
| COMMIT YOK | ✅ | Çalışma ağacında kalındı |

---

## 10. Kurucuya / mali müşavire notlar

1. **Hava kaynağı = KURUCU KALEMİ.** Seçilen kaynak **Open-Meteo**
   (`open-meteo.com`). Ücretsiz katman **CC BY 4.0** ve **ticari olmayan**
   kullanım içindir; ticari kullanım **ücretli plan/sözleşme** ister. Bu yüzden
   `Hava:Etkin` **varsayılan false**'tur ve sessizce açılmaz. Karar verilene
   kadar müşteride hava kutucuğu **görünmez** (ekran bundan etkilenmez).
2. **Hava açılacaksa iki ayar birlikte verilir** (biri eksikse kutucuk sessizce
   düşer, hata çıkmaz):
   `Hava:Etkin=true` + `Hava:Sehir` + `Hava:Url` (istemci, `wwwroot/appsettings.json`)
   **ve** `Hava__KaynakOrigin=https://api.open-meteo.com` (sunucu — CSP izni).
   Origin'i açmak bir **güvenlik kararıdır** (Mehmet'in kapısı): tek bir https
   origin açılır, yol/sorgu/boşluk taşıyan değer reddedilir.
3. **Doğrulama ortamının ağ kısıtı:** ajan tarayıcısı dış internete çıkamıyor.
   Gerçek Open-Meteo yanıtının ekranda göründüğü, kurucunun kendi tarayıcısında
   (5199 veya üretim) **bir kez** teyit edilmelidir.
4. **Artık dosyalar (P-37a'dan devreden, silme kurucuda):** ağaçta 3 `.bak` ve
   çok sayıda `.mutorj` var; hook `rm`/`mv`'yi engellediği için ekip silemiyor.
   Bu pakette **yeni artık doğmadı** (denetim sayacı 0).
5. **T-3 (tekrar):** MySQL root parolası `docker-compose.yml` içinde düz metin;
   `.env`e taşınması kurucu kalemidir. (Bu koşumda değeri hiçbir çıktıya
   yazılmadı, yalnız değişkende kullanıldı.)
6. **Sistem anahtarı:** canlı doğrulama için `Sistem__Anahtari` **yalnız kendi
   5198 sürecimin ortam değişkeninde** üretildi ve süreçle birlikte gitti;
   hiçbir dosyaya kalıcı yazılmadı, iş bitince boşaltıldı.

---

## 11. Değişen dosyalar

```
M  dotnet/Kuyumcu.Api/Program.cs                       (CSP hava kapısı)
M  dotnet/Kuyumcu.Web/Pages/Panel.razor                (hava durumu → sözlük)
M  dotnet/Kuyumcu.Web/wwwroot/css/app.css              (ölçüt 1: iki sütun)
?? dotnet/Kuyumcu.Api/Common/HavaOrigin.cs             (YENİ)
?? dotnet/Kuyumcu.Core/I18n/HavaSozlugu.cs             (YENİ)
?? dotnet/Kuyumcu.Web/Servisler/HavaServisi.cs         (yarım koşumdan, bu koşumda i18n'e bağlandı)
?? dotnet/Kuyumcu.Web/Servisler/PanelKutucuklari.cs    (yarım koşumdan)
?? dotnet/Kuyumcu.Tests/P37bKarsilamaTestleri.cs       (20 → 31 test)
?? dotnet/denetim.sh                                   (KARSILAMA_TERCIH_KULLANICI_DISI sayacı)
   dokumanlar/09-uygulama-kayitlari/PAKET-P-37b-uygulama-raporu.md (bu dosya)
```
`Kuyumcu.Web/wwwroot/appsettings.json` mutasyon/canlı doğrulama için geçici
değiştirildi ve **`cmp` ile birebir geri alındı** (net değişiklik yok).

---

## 12. Açık kalanlar

1. **Gerçek hava yanıtı canlıda görülmedi** (ortam ağ kısıtı) — kurucu kendi
   tarayıcısında bir kez teyit etmeli.
2. **Hava kaynağı lisansı** — sözleşme/ücretli plan kararı verilmeden kutucuk
   kapalı kalır.
3. **1280 px dropdown kesişimi (5056 px²)** — P-37a'dan devreden, **bu paketin
   kapsamı değil**, ayrı pakette duruyor.
4. **Ekran görüntüsü arşivi** — görüntüler oturum içinde alındı; kalıcı dosya
   olarak arşivlenmesi için proje ağacına ikili dosya bırakılmadı (kural).

---

## 13. § Kimlik kapanış tablosu

| Eleman | Kimlik | Not |
|---|---|---|
| Karşılama başlığı | `panel-karsilama` · `panel-selam` · `panel-tarih` | yeni |
| Hava | `panel-hava` · `panel-hava-sehir` · `panel-hava-derece` · `panel-hava-durum` | yeni |
| Izgara | `panel-izgara` | sürükle-bırak kabı |
| Kutucuk | `panel-kutucuk-{kod}` · `panel-kutucuk-{kod}-baslik` | kod: gun-ozeti·kur·bekleyen·kasa·hizli-eylem·son-islemler·grafik |
| Düzen | `panel-duzen-modu` · `panel-duzen-sifirla` · `panel-duzen-hata` · `panel-gizli-liste` · `panel-gizle-{kod}` · `panel-goster-{kod}` | yeni |
| İskelet | `panel-iskelet-{kod}` | 6 adet |
| Hızlı eylem | `panel-satis-yap` · `panel-alis-yap` · `panel-tahsilat` · `panel-hurda` | yeni |
| Bantlar / bağlantılar | `hata-bandi` · `panel-defter-hata` · `panel-bekleyen-hata` · `panel-grafik-hata` · `panel-gun-ozeti-git` · `panel-kasa-git` · `panel-defter-git` | — |
| Korunan kimlik | `kur-kaynak` | P-36'dan **birebir** korundu, değiştirilmedi |
