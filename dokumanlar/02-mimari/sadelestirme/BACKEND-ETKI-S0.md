# S-0 · BACKEND ETKİ ANALİZİ — "Bu front tasarımı backend'i nasıl etkiler?"

> **Soru sahibi:** Kurucu (23 Eylül). **Kapsam:** `PLAN-SADELESTIRME-PUSULA-2026-09-23.md` § 5 (iki yüz tek gövde), § 6 S-2, § 6.0 D1–D11.
> **Yöntem:** KODA BAKILARAK. Her satırın kanıtı dosya+satır olarak yanında. Ölçülemeyen "ÖLÇÜLEMEDİ" yazıldı (Anayasa md. 1).
> **Bu belgede kod yazılmadı, hiçbir `.cs`/`.razor` değiştirilmedi.** Yalnız bu dosya üretildi.
> **Ağaç:** `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery` → sembolik bağ → `/Users/ramazanakkocan/ByGatech-yerel/jewellery` (aynı ağaç, aynı git kökü `bbfdc95 [main]`).

---

## 0. TEK CÜMLELİK CEVAP

**Sadeleştirme planının büyük kısmı (S-1 kabuk, S-3/S-4 ekran şablonları, D4 üst bar, D8 menü katlama, D11 menü zemini) backend'e HİÇ dokunmaz — saf istemci işidir.** Backend'e dokunan **yalnız 5 kalem** vardır: **D1** (varsayılan mod), **D2** (Peşin Müşteri carisi), **D3** (karma ödemede ALTIN bacağı), **D10** (sözlük — kod değişir, şema değişmez), ve **tema/şema tercihinin saklanması** (§ 5A). Bunlardan **ikisi para/defter yolundadır (D2, D3)** ve **D3 tek başına planın en riskli maddesidir** — çünkü plan "ödeme türü zaten var, yalnız kabloyu bağla" varsayıyor, **kod bunun tersini söylüyor**.

---

## 1. ORKESTRATÖRÜN VARSAYIMINDAKİ DÜZELTME (en önemli bulgu)

Görev metni şunu ölçülmüş gerçek olarak veriyordu:

> *"Entities.cs:112 OdemeTurleri = ["NAKIT","KART","HAVALE","ALTIN","DIGER"] → D3'ün ALTIN bacağı ödeme türü olarak ZATEN VAR; iş yeni tür eklemek değil, has defteri yazma yolunu bağlamaktır."*

**Bu yarı doğrudur ve D3'ü olduğundan küçük gösterir.** Kodda **İKİ AYRI ÖDEME LİSTESİ** vardır:

| Liste | Nerede | İçerik | ALTIN var mı |
|---|---|---|---|
| **Tek etiketli ödeme** (`POST /islemler/satis` vb.) | `Kuyumcu.Api/Modules/Satis/SatisModulu.cs:59` — `[RegularExpression("NAKIT\|KART\|HAVALE\|ALTIN\|VERESIYE")]` | 5 değer | **EVET** |
| **Karma / çok bacaklı fiş** (`POST /islemler/satis-fisi`) | `Kuyumcu.Api/Modules/Satis/OdemeSepeti.cs:17` — `[RegularExpression("NAKIT\|KART\|HAVALE\|VERESIYE\|DOVIZ")]` ve `Kuyumcu.Api/Data/OdemeKalemi.cs:91` — `OdemeAraci.Tumu = [Nakit, Kart, Havale, Veresiye, Doviz]` | 5 değer | **HAYIR — BİLEREK YOK** |

D3'ün konusu **karma ödemedir** (plan § 6 S-2.5: *"karma ödeme basit modda açık + ALTIN bacağı"*). Yani iş, **var olan bir bacağın kablosunu bağlamak değil, listede bilerek olmayan bir bacağı açmaktır.** Kaynaktaki gerekçe aynen şudur (`OdemeSepeti.cs:12-15`):

> "ALTIN BİLEREK YOKTUR. Tek etiketli ALTIN yolu bugün TL kasasına TL yazıyor ve tevsik kapısını hiç tetiklemiyor; bunun bilinçli bir muhasebe kararı mı yoksa boşluk mu olduğu belgelenmemiştir. Belgelenmemiş bir davranışın üstüne bacak inşa edilmez (Anayasa md.1)."

Bunu koddan **doğruladım**:
- `SatisModulu.cs:669-671` — kasa doğrulaması **"TRY"** sabitiyle yapılır (`KasaDogrula(..., "TRY", ...)`): ALTIN etiketli satış TL kasasına TL yazar.
- `SatisModulu.cs:674` — tevsik kapısı **yalnız** `dto.OdemeTuru == "NAKIT"` iken çağrılır. → ALTIN ödeme **459 s. VUK tevsik kapısını hiç görmez.**
- `HareketFabrikasi.cs:53, 59-63` — `OdemeIzi.Etiketten` içinde `"KART"/"HAVALE"/"ALTIN"` hepsi `Yok` (nakit payı 0, veresiye payı 0) döner. ALTIN'ın **has defterine dair hiçbir izi yoktur.**
- `SatisModulu.cs:716-720` — satışta `HasGrami` yalnız **ürün stok çıkışıdır**; kaynak yorumu aynen: *"ALTIN toplamına karışamaz."*
- **Kural testle tutuluyor:** `Kuyumcu.Tests/P23OdemeKalemiTestleri.cs:56` → `Assert.False(OdemeAraci.Gecerli("ALTIN"));` ve `:40` → DTO araçları ile `OdemeAraci.Tumu` **tam eşit** olmak zorunda. `Kuyumcu.Tests/IadeTestleri.cs:547` → `Iade_odeme_secicisinde_ALTIN_YOKTUR`.

**Sonuç:** D3 "kablo işi" değil, **muhasebe kararıdır**; planın § 6.0 D3 satırındaki "önce bugünkü davranış belgelenir" şartı **zorunludur ve atlanamaz**. Belgeleme, "ALTIN ödeme has defterine mi yoksa TL defterine mi işler" sorusunun **kurucu/mali müşavir cevabını** gerektirir — kodda cevabı YOKTUR.

---

## 2. KALEM KALEM ETKİ TABLOSU

Sütunlar: (a) backend'e dokunuyor mu · (b) dosya/tablo/uç · (c) migration · (d) para/defter yolu · (e) kırılabilecek testler.

### 2.1 § 6.0 kararları (D1–D11)

| # | Karar | (a) Backend? | (b) Dosya / tablo / uç | (c) Migration | (d) Para yolu | (e) Kırılabilecek test |
|---|---|---|---|---|---|---|
| **D1** | Varsayılan mod BASİT | **EVET (küçük)** | `Data/Entities.cs:148` (`= "PROFESYONEL"`), `Data/KuyumcuDbContext.cs:751` (`HasDefaultValue("PROFESYONEL")`), `Modules/Kullanici/ArayuzModu.cs:36-40` (`Coz` geri düşüşü), `Modules/Auth/AuthModulu.cs:285,356,449` | **EVET — 1 adet** (kolon VAR ama `HasDefaultValue` model anlığında tutulur; değer değişirse EF model değişir, bekleyen-migration kapısı `denetim.sh:1972` kırmızı verir). **Kolon eklenmez, yalnız DEFAULT değişir.** | **HAYIR** | `Kuyumcu.Tests/ArayuzModuTestleri.cs:84` (`Assert.Equal("PROFESYONEL", tenant.VarsayilanArayuzModu)`), `:86`, `IsletmeBilgileriTestleri.cs:184`. Ayrıca 7 test dosyası "PROFESYONEL" dizgisi taşıyor (ArayuzModu, BasitModMenu, P16Ekran, P38SahipsizBulgular, ProAlanEkran, UX1C4Ekran, YedekEkran) |
| **D1-ek** | *"TEZGÂHTAR rolü her kiracıda BASİT"* | **EVET — planda yazılandan büyük** | `ArayuzModu.Coz(kullaniciModu, kiraciVarsayilani)` bugün **rol parametresi ALMAZ** (`ArayuzModu.cs:36`). Rol `kullanicilar.rol` (`Entities.cs:285`, varsayılan `"TEZGAHTAR"`) kolonunda, ama çözüm yoluna hiç girmiyor. Rolü katmak = imza değişikliği + 3 çağrı yeri | HAYIR (kolon var) | HAYIR | `ArayuzModuTestleri.cs` tamamı (çözüm sözleşmesi testleri) |
| **D2** | "Peşin Müşteri" sistem carisi | **EVET — en geniş backend işi** | `Data/Entities.cs:340-372` `Cari` sınıfında **`sistem_carisi` kolonu YOKTUR** (grep: kod tabanında `SistemCarisi`/`sistem_carisi` = **0 eşleşme**); `Modules/Cari/CariModulu.cs` (silme/pasife alma kapısı), `Modules/Satis/SatisModulu.cs` (tevsik + MASAK kapısı), kiracı tohumlama (ADR-0027) | **EVET — 1 adet** (`cariler.sistem_carisi` bool + tohumlama) | **EVET (dolaylı)** — cari bakiye/veresiye yolu; "fark ≠ 0 kaydedilemez" kuralı | Cari silme/pasife alma testleri, MASAK/tevsik kümülatif testleri (`MasakServisTestleri`, `TevsikTestleri`, `MasakKuralTestleri`, `MasakBaglantiliIslemTestleri`), kiracı tohumlama testleri |
| **D3** | Karma ödemeye ALTIN bacağı | **EVET — EN RİSKLİ** | `Modules/Satis/OdemeSepeti.cs:17` regex, `Data/OdemeKalemi.cs:91` `OdemeAraci.Tumu`, `Data/OdemeKalemFabrikasi.cs:73`, `Data/HareketFabrikasi.cs:59-63` `OdemeIzi.Etiketten`, `SatisModulu.cs:669-676` (kasa "TRY" + tevsik yalnız NAKIT), `Modules/Vezne/VezneModulu.cs:68,682,705` (**aynı DTO'yu kullanıyor — yan etki buraya sıçrar**) | **MUHTEMELEN EVET** — has bacağının gram/ayar bilgisi `odeme_kalemleri`de kolon ister (bugün yalnız `TutarTl`, `ParaBirimi`, `DovizTutar` var). **ÖLÇÜLEMEDİ:** tasarım yapılmadan kolon sayısı söylenemez, tahmin edilmez | **EVET — doğrudan has defteri + kasa + tevsik** | `P23OdemeKalemiTestleri.cs:40,56` (**kesin kırılır** — ALTIN eklenince `Assert.False(Gecerli("ALTIN"))` çöker), `IadeTestleri.cs:547`, `KarmaOdemeEkranTestleri.cs`, tüm `Masak*`/`Tevsik*` süitleri, `denetim.sh:5068` `FIS_TOPLAM_ODEME_KALEMI_FARKI` canlı kapısı |
| **D4** | Üst bar sadeleştirme | **HAYIR** | Yalnız `Kuyumcu.Web/Layout/UstBar.razor`. Kur şeridi mevcut uçtan besleniyor | yok | hayır | UstBar bUnit testleri (istemci) |
| **D5** | UX-2-B/3-C/2-D'nin dalgaya çekilmesi | **HAYIR** (belge işi) | `MIMARI-KALAN-PAKETLER-2026-09-15.md` | yok | hayır | yok |
| **D6** | Şema B/C'de başarı rengi | **HAYIR** | `belirtecler.css` | yok | hayır | `TasarimBelirtecTestleri` (kontrast) |
| **D7** | Kapsam istisnaları (para üstü, Açık Pozisyon kutucuğu) | **KISMEN** | Para üstü = **istemci hesabı** (kasaya giren yine fiş toplamı; `KasaGuncelle` `SatisModulu.cs:2322` işaretli yazıyor, para üstü ayrı bacak DEĞİL). "Açık Pozisyon" kutucuğu = mevcut rapor ucundan okuma. **ÖLÇÜLEMEDİ:** hangi rapor ucunun açık pozisyonu verdiği bu turda aranmadı | yok | para üstü **ekranda**, defterde değil → düşük risk | Kasa/rapor testleri (düşük) |
| **D8** | Menü grup katlama tercihi | **EVET — çok küçük** | `Modules/Tercih/TercihModulu.cs:26-36` — `TercihAnahtarlari.Tumu` **beyaz listedir** (`menu-favoriler`, `menu-sirasi`, `panel-duzeni`) ve `:58` listede olmayan anahtarı **reddeder**. Yeni anahtar (`menu-gruplar`) **eklenmelidir**. Uç aynı kalır (`PATCH api/v1/tercihler/benim`) → planın "yeni uç yok" ifadesi DOĞRU | **HAYIR** (değer satır olarak yazılıyor, şema değişmiyor) | hayır | `Tercih*` testleri (beyaz liste sayımı varsa) |
| **D9** | Vendored yazı tipi | **HAYIR** | `wwwroot/font/`, CSP başlığı (`Program.cs` CSP satırı — **ÖLÇÜLEMEDİ**, bu turda CSP satırı okunmadı) | yok | hayır | CSP/yayın dosya sözleşmesi testleri |
| **D10** | Sözlük göçü | **EVET ama şema değil** | `Kuyumcu.Core/I18n/Sozlukler.cs` (9.217 satır, tek dosya). API'ye dokunmaz | **HAYIR** | hayır | **Ölçüm:** bUnit'te metinle bulma (`:contains`) **0 eşleşme, 0 dosya** → testler **id ile buluyor**; sözlük göçünün test kırma riski **planda sanılandan düşük**. (Plan § 5B'nin "grep -c contains" korkusu ölçüldü ve boş çıktı.) |
| **D11** | Kenar çubuğu zemini | **HAYIR** | `belirtecler.css` / `KenarCubugu.razor` | yok | hayır | kontrast testleri |
| **§ 5A** | **Tema/şema/yazı boyutu seçimi (8 şema × açık/koyu × 3 punto)** | **EVET — planda backend kalemi olarak sayılmamış** | Plan "`kullanicilar.tema/sema/yazi_boyutu`" diyor; **bu üç kolon bugün YOKTUR** (`Entities.cs:278-296`: yalnız `arayuz_modu` var). **İki yol:** (1) 3 kolon + kiracı varsayılanı → **migration**; (2) mevcut `KullaniciTercihleri` tablosuna yeni beyaz-liste anahtarı → **migration YOK**. Mimari öneri: yol (2), D8 ile aynı mekanizma | (1) EVET · (2) **HAYIR** | hayır | Tercih testleri |

### 2.2 § 6 S-2 kalemleri (Tezgâh fişi)

| # | İş | (a) Backend? | (b) Nerede | (c) Migration | (d) Para yolu | (e) Test riski |
|---|---|---|---|---|---|---|
| **2.1** | `Islemler.razor` bileşenlere bölünmesi (2.576 satır) | **HAYIR** | Saf istemci | yok | **hayır — DOĞRULANDI** | id envanteri + POS bUnit süiti |
| **2.2** | Sekme → düğme ızgarası | HAYIR | `BasitPos.cs`, `Islemler.razor` | yok | hayır | `BasitModPosTestleri` |
| **2.3** | Tek grid VERDİM/ALDIM + FARK | **KISMEN** | Fark hesabı istemcide; ama "Peşin Müşteri'de fark ≠ 0 kaydedilemez" kuralı **sunucuda da** olmalı (fail-closed) → `OdemeSepeti.cs` doğrulayıcısı | D2'ye bağlı | **EVET** | `OdemeSepeti` doğrulama testleri |
| **2.4** | Peşin Müşteri | **EVET** | D2 ile aynı | **1** | EVET | D2 ile aynı |
| **2.5** | Karma + ALTIN bacağı | **EVET** | D3 ile aynı | D3 ile aynı | **EVET (en yüksek)** | D3 ile aynı |
| **2.6** | Bakiye rozeti | HAYIR | Veri zaten yükleniyor (`CariBakiye`, `Entities.cs:441-456`: `tl_bakiye`, `has_gram_bakiye` mevcut) | yok | hayır | — |
| **2.7** | Kaydet → Yazdır | **HAYIR** | Uç **HAZIR**: `Modules/Cikti/CiktiModulu.cs:317,323` → `GET /api/v1/cikti/{tur}/{id}`. Yalnız istemciden bağlanacak | yok | hayır | — |
| **2.8** | Barkod odağı + para üstü + personel | KISMEN | Para üstü D7 (istemci). `personel_id` **ÖLÇÜLEMEDİ** (bu turda hareket kolonları taranmadı) | ÖLÇÜLEMEDİ | düşük | — |
| **2.9** | **Pro modda P-21 işlemleri** | **HAYIR — uçlar hazır** | Aşağıdaki sayım | yok | hayır | — |

#### S-2.9 · P-21 uçları GERÇEKTEN hazır mı — SAYILDI

`Kuyumcu.Api/Modules/Satis/SatisModulu.P21.cs` (1.285 satır), taban yol `[Route("api/v1/islemler")]` (`:1204`). **12 uç** (`grep -c "\[Http"` = 12):

| # | Fiil | Yol | Satır |
|---|---|---|---|
| 1 | POST | `rezervler` | 1213 |
| 2 | GET | `rezervler` | 1219 |
| 3 | PATCH | `rezervler/{id}` | 1226 |
| 4 | POST | `rezervler/{id}/iptal` | 1232 |
| 5 | POST | `avans` | 1240 |
| 6 | POST | `avans/mahsup` | 1246 |
| 7 | POST | `avans/iade` | 1251 |
| 8 | GET | `avans/{cariId}` | 1256 |
| 9 | POST | `degisim` | 1263 |
| 10 | POST | `konsinye-cikis` | 1271 |
| 11 | POST | `konsinye-iade` | 1277 |
| 12 | GET | `konsinye-acik` | 1282 |

+ **13.** `POST api/v1/evraklar/{id}/rucu` — `Modules/Evrak/EvrakModulu.cs:1149` (ciro/rücu bacağı, P-21 dosyasında değil).

**Planın "14 uç" rakamı DOĞRULANMADI: 12 (+1 evrak rücu) = 13 sayıldı.** 14'üncü olarak anılan **"siparişten satış"** ucu `Modules/` altında bulunamadı (`siparisten|SiparistenSatis` grep = 0 eşleşme). → **Plan § 6 S-2.9'daki "14 uç" 13 olarak düzeltilmeli; "siparişten satış" ya başka adla var ya hiç yok — ÖLÇÜLEMEDİ, S-2 öncesi Bertan doğrulamalı.**

**"Uç var, düğme yok" (UX-S25) teyidi:** `Kuyumcu.Web` içinde `islemler/rezervler`, `islemler/avans`, `islemler/degisim`, `islemler/konsinye`, `/rucu` çağrısı **0 eşleşme**. → Bu 13 uç bugün **ekrandan hiç çağrılamıyor**; S-2.9 saf istemci kablo işidir, backend'e dokunmaz.

#### S-2.1 · "Tek POST yolu" kuralı — KODDAN DOĞRULANDI

`Kuyumcu.Web/Pages/Islemler.razor` = **2.576 satır**. Dosyadaki **tüm** `Api.PostAsync` çağrıları (9 adet) `Kaydet()` gövdesinin içindedir:

| Satır | Uç |
|---|---|
| 2246 | `private async Task Kaydet()` — **gövde başı** |
| 2274 | `POST /islemler/{uc}` (dinamik) |
| 2290, 2333 | `POST /islemler/satis-fisi` |
| 2357 | `POST /islemler/satis` |
| 2376 | `POST /islemler/alis` |
| 2396 | `POST /islemler/hurda-alis` |
| 2421 | `POST /islemler/takas` |
| 2439 | `POST /islemler/transfer` |
| 2454 | `POST /islemler/iscilik` |

**`Kaydet()` dışında tek bir POST yoktur.** → Bertan KOŞUL 4 bugün **sağlanıyor**; S-2.1'in bileşenlere bölünmesinde korunması gereken tek şey, bu 9 çağrının `Kaydet()` içinde kalması ve yeni `Bilesenler/Fis/*` bileşenlerine `ApiIstemci` **enjekte edilmemesidir**. Bu kural `denetim.sh:107`'de ("Hareketler.Add kuralının POS tarafındaki karşılığı: ikinci bir yazma yolu…") zaten sayaçla korunuyor.

---

## 3. SAYILAR

| Ölçü | Değer | Nasıl ölçüldü |
|---|---|---|
| Sadeleştirme planının backend'e dokunan kalem sayısı | **5** (D1, D2, D3, D8, § 5A tema saklama) + D1-ek rol kuralı | § 2 tabloları |
| Backend'e HİÇ dokunmayan kalem | D4, D5, D6, D9, D10 (şema), D11, S-2.1, S-2.2, S-2.6, S-2.7, S-2.9 | § 2 tabloları |
| **Dokunulan tablo sayısı** | **3–4**: `cariler` (D2, kolon ekleme) · `tenants` (D1, yalnız DEFAULT) · `kullanici_tercihleri` (D8 + § 5A, **satır**, şema değil) · `odeme_kalemleri` (D3, **ÖLÇÜLEMEDİ** — tasarıma bağlı) |
| **Migration sayısı** | **2 kesin** (D2 `cariler.sistem_carisi`; D1 default değişimi) **+ 1 olası** (D3 has bacağı kolonları — ÖLÇÜLEMEDİ) **+ 1 kaçınılabilir** (§ 5A tema: tercih anahtarı seçilirse **0**) → **2 ile 4 arası** |
| Mevcut migration sayısı (bugün) | **72** (`ls Kuyumcu.Api/Data/Migrations/*.cs \| grep -v Designer \| grep -v Snapshot \| wc -l`). ORKESTRATOR-DURUM 22.09'da 66 diyor; aradaki 6 diğer şeritlerin. |
| Toplam test dosyası | **269** (`ls Kuyumcu.Tests/*.cs \| wc -l`) |
| Metinle bulan bUnit testi (`:contains`) | **0 dosya, 0 eşleşme** → D10 sözlük göçünün test riski düşük |
| P-21 uçları | **13** sayıldı (plan 14 diyor) · ekrandan çağrı **0** |
| `Islemler.razor` | 2.576 satır · 9 POST, hepsi `Kaydet()` içinde |

---

## 4. RİSK SIRASI (yüksekten düşüğe)

1. **D3 — karma ödemede ALTIN bacağı. AĞIR RİSK.** Has defteri + kasa + 459 s. VUK tevsik + MASAK kümülatifi aynı anda. Kaynak, ALTIN'ın bugünkü davranışını **"belgelenmemiş"** ilan etmiş ve bacağı bilerek kapatmış. Aynı DTO'yu Vezne de kullanıyor (`VezneModulu.cs:68`) → yan etki ikinci modüle sıçrar. `P23OdemeKalemiTestleri.cs:40,56` **kesin kırılır** (kırılması beklenen, ama sözleşme testidir: değişimi Mehmet+Bertan oyu olmadan yapmak yasak).
2. **D2 — Peşin Müşteri.** Migration + fail-closed yasal kapı. Risk, kolonun kendisinde değil, **"sistem carisi eşik üstünde reddedilir"** kuralının sunucuda eksiksiz kurulmasında. İstemcide kalırsa MASAK kapısı delinir (§ 3 madde 6 ihlali).
3. **S-2.3 FARK kuralı** — D2'nin türevi; "fark ≠ 0 kaydedilemez" istemcide kalırsa kasa/bakiye saptırır.
4. **D1 — varsayılan mod.** Teknik olarak küçük (1 migration, 3 test satırı), ama **`ArayuzModu.cs:29-35`'teki mimari gerekçenin tersine çevrilmesidir**: "bilinmeyen durumda alan gizlemek, gizlenen alanın hiç doldurulmaması riskini doğurur." D1 bu gerekçeyi çürütmeden geçirilemez → **Bertan + Mehmet oyu şart.** (Not: `Coz`'un *geri düşüşü* PROFESYONEL kalabilir; değişen yalnız yeni kiracının DEFAULT'u olursa gerekçe korunur — önerilen okuma budur.)
5. **D1-ek rol kuralı** — `Coz` imza değişikliği, 3 çağrı yeri, sözleşme testleri.
6. **D8 + § 5A tercih anahtarları** — beyaz listeye 2 anahtar; migration yok, risk düşük.
7. **D10 sözlük** — tek dosya, şema yok, `:contains` = 0 → **en düşük riskli backend-yakını iş**.
8. **Geri kalan her şey (S-1, S-3, S-4, D4, D6, D9, D11, S-2.1/2.2/2.6/2.7/2.9)** — backend riski **sıfır**.

---

## 5. SIRALAMA KISITLARI (bağlayıcı)

1. **Para yoluna dokunan işler (D2, D3, S-2.3, S-2.4, S-2.5) P-45 (altın/gümüş has ayrımı) BİTMEDEN BAŞLAMAZ.** Gerekçe: ALTIN bacağının has defterine hangi madenle gireceği P-45'in çıktısına bağlıdır; P-45 öncesi yazılan bacak, P-45 sonrası geriye dönük veri düzeltmesi gerektirir. **NOT — ÖLÇÜLEMEDİ:** P-45'in bugünkü durumu (bitti / sırada / iptal) bu turda doğrulanmadı; S-2 açılmadan `ORKESTRATOR-DURUM.md` üzerinden teyit edilmeli.
2. **D3, "bugünkü ALTIN davranışının belgelenmesi" bitmeden yazılmaz** (plan § 6.0 D3'ün kendi şartı). Belge, `OdemeSepeti.cs:12-15`'teki açık soruyu — *ALTIN ödeme has defterine mi TL defterine mi işler* — mali cevapla kapatmalıdır. Cevap yoksa D3 **ertelenir**, S-2.5 dalgadan çıkarılır; S-2'nin geri kalanı (2.1–2.3, 2.6–2.8) yine yazılabilir (planın kendi risk notu da böyle diyor).
3. **D2 (migration + kapı) ile S-2.3 (FARK kuralı) aynı pakette gider.** Ayrılırsa, ön seçili sistem carisi var ama eşik kapısı yok bir ara durum doğar — **MASAK açığı**. Mehmet veto alanı.
4. **D1, S-1 ve S-3 bitmeden AÇILMAZ.** Varsayılanı BASİT yapmak, basit mod ekranları hazır değilken yeni kiracıyı yarım yüze düşürür. D1 sıralamada **S-3 sonrası**dır.
5. **S-2.1 (bölme) ile S-2.5 (ALTIN) aynı anda yazılmaz.** Yaz-seri kuralı: `Islemler.razor` hem bölünürken hem yeni bacak alırsa, kırılan bUnit'in sebebi ayrıştırılamaz. Önce 2.1 + id envanteri diff boş, sonra bacak.
6. **D8 ve § 5A tercih anahtarları tek dokunuşta** `TercihAnahtarlari.Tumu`'ya eklenir (iki ayrı pakette iki kez aynı beyaz listeye dokunmak yaz-seri ihlalidir).
7. **Üç kod şeridiyle çakışma:** D3/D2 `Modules/Satis` + `Modules/Vezne`'ye dokunur; **PARA DOĞRULUĞU şeridi `Modules/Vezne`, `Modules/Kasa`, `Modules/Maliyet`de koşuyor.** → S-2.4/2.5, para doğruluğu şeridi bu dosyaları bırakmadan başlamaz. Orkestratörün teyidi şart (plan § 3 madde 9).

---

## 6. KURUCUYA KISA CEVAP

**"Bu front tasarımı backend'i nasıl etkiler?"**

Neredeyse hiç. Planın 40+ kaleminin **beşi** sunucuya dokunuyor, **ikisi** migration istiyor (`cariler.sistem_carisi` ve `tenants` varsayılanı), **biri** — karma ödemede altınla ödeme — gerçekten ağır: çünkü kodda altın bacağı **kazara eksik değil, bilerek kapalı**; açmak için "altınla ödenen para has defterine mi girer" sorusunun mali cevabı gerekiyor, ve bu cevap **bizde yok**. Geri kalan her şey — menü, renkler, yazı, ekran düzeni, satış fişinin yüzü, pro moddaki 13 gizli işlem — **sunucuya hiç dokunmadan** yapılır; o 13 işlemin ucu zaten sunucuda hazır, yalnız düğmesi yok.

---

**Ölçüm tarihi:** 2026-09-23 · **Yazan:** Bertan (02 Baş Mimar) merceği, S-0 dalgası · **Kod değişmedi, commit yok.**
