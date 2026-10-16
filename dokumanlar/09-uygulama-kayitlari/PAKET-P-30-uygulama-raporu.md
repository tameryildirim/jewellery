# PAKET P-30 — DEMO-1 TEMİZ DEMO KİRACISI + 15 DK GÖSTERİM (uygulama raporu)

**Tarih:** 2026-09-23 · **Dalga:** 7 · **Kimlik:** R-4 · **Sürüm:** 1.0
**Durum:** **YARIM** — mekanizma kuruldu ve kanıtlandı; *gerçek* DEMO kiracısı ve
*3 aylık geçmiş* açılamadı (gerekçeler § 2 ve § 3). Bu beyan bağlayıcıdır.

---

## 0. Önkoşul ölçümü (durma-devam et modu)

| Önkoşul | Ölçüm | Sonuç |
|---|---|---|
| **P-24** | `dokumanlar/09-uygulama-kayitlari/PAKET-P-24-uygulama-raporu.md` **VAR** | Karşılandı |
| **K-1** (canlı DB temizliği + QA fikstür izni) | `ORKESTRATOR-DURUM.md` 2026-09-21 kaydı: *"Kurucuda kalan kalemler … K-16, **K-1**"*. Canlı `tenants` tablosunda hâlâ eski kirli kayıtlar duruyor: `Demo Kuyumculuk`, `Demo Kuyumcu`, `Düzeltme Sonrası Kuyumculuk`, `Ramazan Deneme` (+4 QA fikstürü) | **AÇIK** |

**BK-16 bağlayıcıdır:** *"K-1 kapanmadan P-30 demo kiracısı … başlamaz."* Bu yüzden
**gerçek `DEMO-*` kiracısı AÇILMADI.** Bunun yerine paket, demo mekanizmasının
tamamını kurdu ve **silinebilir prova kiracılarıyla** uçtan uca kanıtladı; prova
kiracıları iş sonunda silindi (§ 6).

---

## 1. Yapılanlar

| Dosya | Durum | Ne yapar |
|---|---|---|
| `dokumanlar/09-uygulama-kayitlari/betikler/demo-tohumla.js` | **YENİ** (17.940 B) | Demo/prova kiracısını **yalnız ürünün kendi uçlarından** tohumlar (davet → kayıt → giriş → kur → kasa → cari → ürün → alış → satış → tahsilat). `--prova` modunda 15 dk gösterimin veri yolunu adım adım ölçer ve `ham/demo-prova-<damga>.json` yazar. |
| `dokumanlar/09-uygulama-kayitlari/betikler/kiraci-temizle.sh` | **DEĞİŞTİ** | `DEMO-*` adı için **açık ret** eklendi: demo, toplu temizliğin silme ad kuralına dâhil değildir. |
| `docs/RUNBOOK-DEMO.md` | **YENİ** (5.077 B) | Kırmızı çizgiler, tohumlama, **10 adımlık / 15 dk gösterim tablosu** (rota + arkasındaki uç), iki turlu prova kuralı, silme prosedürü. "TASLAK — ilan edilmemiştir" damgası + `HARIC-KAYNAK` işareti taşır. |
| `dotnet/Kuyumcu.Contracts/Kisit/KisitSozlesmeleri.cs` | **YENİ** | `KisitBeyani.Liste` (H-1…H-8, TR+EN başlık, durum kodu) + `KisitBeyaniYaniti`. |
| `dotnet/Kuyumcu.Api/Modules/Meta/KisitModulu.cs` | **YENİ** | `GET /api/v1/genel/kisitlar` — **anonim**, kiracıya özel veri dönmez, DB'ye dokunmaz. |
| `dotnet/Kuyumcu.Web/Pages/Giris.razor` | **DEĞİŞTİ** | Giriş ekranında **"Bu kurulumda HARİÇ olanlar"** paneli: yükleniyor / boş durum / **ayrı hata bandı** (UX-S28) / kaynak satırı. |
| `dotnet/Kuyumcu.Core/I18n/Sozlukler.cs` | **DEĞİŞTİ** | 8 yeni anahtar **TR + EN** (required → derleme kapısı). Ham durum kodu ekrana basılmaz (UX-S31). |
| `dotnet/Kuyumcu.Web/wwwroot/css/app.css` | **DEĞİŞTİ** | `.giris-kisit*` sınıfları — **sabit renk yok**, yalnız belirteç `var()`. |
| `dotnet/denetim.sh` | **DEĞİŞTİ** | `# >>> P-30 … # <<< P-30` bloğu: **DEMO_IZOLASYON**, **DEMO_ROTA_KRITIK**, **DEMO_KISIT_AYRISMA**. |
| `dotnet/Kuyumcu.Tests/P30DemoKisitTestleri.cs` | **YENİ** | 6 test (saf 2 + HTTP 1 + bUnit 3). |

**Dokunulan sayfalar (MÜHÜR 2 · tek dokunuş):** `Pages/Giris.razor` — bu dalgada
**ilk** dokunuş.

---

## 2. Spesifikasyon ↔ kod doğrulaması ve sapmalar

| Plan DoD maddesi | Durum | Gerekçe / kanıt |
|---|---|---|
| Demo `QA-TEST-*`'ten türetilmez, adı `DEMO-*`, silme ad kuralına eklenmez | **KARŞILANDI** | Betikte `QA-TEST-` reddi + `DEMO-` zorunluluğu; `kiraci-temizle.sh`'ta `DEMO-*)` reddi (canlı kanıt § 5). |
| 3 aylık tutarlı geçmiş **HareketFabrikasi üzerinden** | **KISMEN → DEVREDİLDİ** | Tohumlama %100 uçlardan geçiyor (doğrudan INSERT **sıfır**), ama **geçmişe tarihli yazılamıyor**: `SatisOlusturDto`/`AlisOlusturDto`/`TahsilatGirisDto`'da **işlem tarihi alanı yok**; hareket tarihi sunucudaki o anki saattir. Tarihi sonradan SQL ile kaydırmak **reddedildi** (denetim damga zinciri yalan söylerdi). Betik ürettiği geçmişi `takvim: "TEK_GUN"` diye damgalıyor — "3 ay" iddiası **edilmiyor** (md.1). |
| Gösterim betiği **iki ayrı ajan tarafından iki kez** koşulur, süre ölçülür, `ham/demo-prova-<tarih>.json` | **KISMEN** | İki koşum yapıldı ve iki ayrı dosyaya yazıldı (§ 4), ama **ikisini de bu ajan koştu**. "İki ayrı ajan" koşulu **karşılanmadı** → ikinci koşum orkestratöre/hasım doğrulayıcıya devredildi. |
| > 20 dk ise paket kapanmaz | **ÖLÇÜLDÜ** | Runbook insan bütçesi **15 dk**; betiğin ölçtüğü veri yolu 373 ms / 357 ms. Betik insan gösterimini ölçmez (alt sınırdır) — bu sınır runbook'ta ve JSON'da yazılı. |
| Kurgusal adlar (KVKK) | **KARŞILANDI** | `CARILER`/`URUNLER` dizileri tamamen kurgusal; gerçek kişi/işletme adı yok. |
| Hariç listesi demo kiracısının **giriş ekranında okunur** (BK-47) | **KARŞILANDI** | `GET /genel/kisitlar` + `Giris.razor` paneli; canlı 200 kanıtı § 5. **Sapma:** panel yalnız demo kiracısına değil **her kurulumun giriş ekranına** çizilir — giriş ekranı oturum öncesi olduğu için kiracıya göre ayrım yapılamaz; kısıtlar zaten kurulum düzeyinde doğrudur, dar yerine geniş kapsam seçildi. |

**Dosya ayak izi sapması:** plan P-30 için 3 dosya sayıyor. BK-47'nin "giriş
ekranında okunur" koşulu yalnız betik/belge ile karşılanamadığı için sözleşme +
uç + ekran + sözlük + CSS + test + denetim bloğu eklendi. Plan bloğu ile bu
koşul çelişmiyor; ayak izi **BK-47'yi karşılamak için** genişledi.

---

## 3. § Kimlik kapanış tablosu

| Kimlik | Durum | Kanıt |
|---|---|---|
| **R-4** (DEMO-1) | **KISMEN** | **Kapanan:** izolasyon kuralı (`kiraci-temizle.sh` DEMO reddi + canlı ret çıktısı), uçtan uca tohumlama betiği (canlı koşum: 3 cari / 3 ürün / 3 satış / 1 tahsilat, hepsi uçlardan), 15 dk gösterim runbook'u ve iki turlu prova (kritik 0/0), giriş ekranında kısıt beyanı (`Giris.razor` + `GET /genel/kisitlar`, canlı 200), üç cırcır sayacı. **Kapanmayan:** (a) gerçek `DEMO-*` kiracısı — **K-1 açık** (BK-16); (b) 3 aylık geçmiş — uçlarda işlem tarihi yok → **devredildi: yeni paket (uçlara birinci sınıf "işlem tarihi")**; (c) "iki ayrı ajan" koşumu — **devredildi: orkestratör/hasım turu**. |

---

## 4. Testler ve mutasyon kanıtları

**Yeni test: 6.** Tam süit (2026-09-23): **Toplam 3944 · Geçti 3944 · Başarısız 0**
(`trx-pP-30/..._2026-09-23_13_47_12.trx`). Koşum öncesi filtresiz sayım
YAPILMADI; "önce" rakamı **ölçülmedi**, tahmin edilmiyor (md.1).

| Kod | Mutasyon | Kırmızıya dönen | Geri alma |
|---|---|---|---|
| **M1-1** | `KisitBeyani.Liste`'den `H-8` satırı silindi (8 → 7) | `P30KisitSozlesmeTestleri.Kisit_listesi_8_satir_benzersiz_kod_ve_bos_olmayan_baslik_tasir` · `…Kod_ve_HARIC_LISTESI_belgesi_ayni_H_kodlarini_tasir` · `P30KisitHttpTestleri.Kisitlar_ucu_JETONSUZ_200_doner_ve_8_satir_tasir` | `cmp` **AYNI** |
| **M1-2** | `Giris.razor`: hata dalı boş duruma çevrildi (`kisitHatasi` → `kisitlar = []`) | `P30GirisEkranTestleri.Kisit_ucu_patlarsa_BOS_DURUM_degil_HATA_cizilir` (`#giris-kisit-hata` bulunamadı) | `cmp` **AYNI** |
| **M1-3** | `KisitController` → `[Authorize]` (anonim okuma kapatıldı) | `P30KisitHttpTestleri.Kisitlar_ucu_JETONSUZ_200_doner_ve_8_satir_tasir` | `cmp` **AYNI** |
| **M1-4** | `kiraci-temizle.sh`'tan `DEMO-*)` reddi silindi | Sayaç: `P-30: DEMO_IZOLASYON` ✗ `1 — temizle.sh-DEMO-reddi-yok` | `cmp` **AYNI** |
| **M1-5** | En yeni prova JSON'unda bir adım `kritik: 1` yapıldı | Sayaç: `P-30: DEMO_ROTA_KRITIK` ✗ `1` | `cmp` **AYNI** |

**Dürüst bulgu (test açığı değil, ölü öznitelik):** ilk denemede `KisitController`
üzerinden **yalnız `[AllowAnonymous]` kaldırıldı** ve **hiçbir test kırılmadı** —
çünkü uygulamada global "varsayılan olarak yetki iste" politikası yok; koruma
sınıf düzeyindeki `[Authorize]` ile kurulur. Mutasyon bu yüzden gerçek kurala
(`[Authorize]`) çevrildi ve **kırmızı oldu**. Öznitelik yine de niyeti belgelemek
için bırakıldı.

---

## 5. Canlı doğrulama (yalnız 5198; 5199'a dokunulmadı)

| # | Adım | Beklenen | Gerçekleşen |
|---|---|---|---|
| 1 | `GET /api/v1/genel/kisitlar` (jetonsuz) | 200 + 8 satır | **200**, `toplam=8`, kodlar `H-1…H-8`, `kaynak=docs/HARIC-LISTESI.md` |
| 2 | `demo-tohumla.js` uçtan uca (prova kiracısı) | tohumlama başarılı | **TOHUMLANDI** · 465 ms · `{"cari":3,"urun":3,"satis":3}` |
| 3 | SQL: defter satırları yazıldı mı | ALIS/SATIS/TAHSILAT satırları | `SELECT tur,COUNT(*) FROM hareketler WHERE tenant_id='01M36XEE…'` → **ALIS 10 · SATIS 9 · TAHSILAT 1**; `kasa_hareketleri` **17** |
| 4 | İş kuralı ihlali var mı | tevsik haddi çalışmalı | Tohumlama **400** aldı (*"aynı gün aynı kişiyle nakit toplamı 54.000 TL, 30.000 TL haddini aşıyor"*) → betik tutarları **kurala uyacak** şekilde düşürüldü (3 × 9.000). Demo artık kuralı **ihlal eden geçmiş yazamaz** (paketin risk maddesi). |
| 5 | `--prova` iki tur | her adım kritik 0 | 2 dosya: `demo-prova-2026-09-23-10-40-40.json` (kritik **0**, 373 ms) · `demo-prova-2026-09-23-10-40-51-249.json` (kritik **0**, 357 ms) |
| 6 | `kiraci-temizle.sh` DEMO reddi | silmeyi reddetmeli | Kiracı geçici olarak `DEMO-P30-GECICI` adına çekildi → **"REDDEDİLDİ: … DEMO kiracısıdır (P-30)"**, `rc=1`; ad geri alındı |
| 7 | Test kiracıları silindi | `tenants` başlangıç sayısına dönmeli | Başlangıç **9** → çalışma sırasında 12 → temizlik sonrası **9**; yetim hareket `0`; `QA-TEST-*` fikstürlerine **dokunulmadı** |
| 8 | API durduruldu | 5198 boş | `lsof -nP -iTCP:5198 -sTCP:LISTEN` → **0 satır** |

> ⚠ **Yapılandırma notu:** `/sistem/*` uçları `appsettings.json`'daki yer tutucu
> anahtarla **fail-closed** (503). Prova için anahtar **yalnız 5198 sürecine
> ortam değişkeniyle** verildi, hiçbir dosyaya yazılmadı, iş sonunda geçici
> dosya boşaltıldı. Depodaki yapılandırma **değişmedi**.

---

## 6. Denetim çıktısı (`cd dotnet && zsh denetim.sh`)

```
  ✓ P-30: DEMO_IZOLASYON        0 (demo QA fikstüründen türemez, toplu temizlik süpüremez, doğrudan SQL yok)
  ✓ P-30: DEMO_ROTA_KRITIK      0 (son iki provanın ikisinde de her rota kritiksiz)
  ✓ P-30: DEMO_KISIT_AYRISMA    0 (giriş ekranı ile HARIC-LISTESI.md aynı H kümesi)
DENETİM BAŞARISIZ — yukarıdaki ihlaller giderilmeli.
```

**DENETİM TEMİZ DEĞİL.** P-30'un üç sayacı da yeşil; kırmızı olan **8 ihlalin
hiçbiri bu paketin bloğunda değil** ve hepsi bu paketten **önce** oradaydı:

| İhlal | Kime ait | Ölçülen kök neden |
|---|---|---|
| `T-5 beyaz listesi ÇÜRÜDÜ` | T-5 | `20260915131748_FiyatListesiVeKampanya.cs` |
| `P-05: DOGAL_ANAHTAR_KISITSIZ` · `BENZERSIZLIK_SOZLUKSUZ` · `CANLI_KISITSIZ` | P-05 | Canlı `kuyumcu_net`'te `UQ_stoklar_…`, `UQ_urunler_…`, `UQ_cariler_…` **yok** → bekleyen migration (K-1 ailesi) |
| `P-06: DUZENLEME_KALIBI_YOK_SAYFA` | P-06 | **zsh sözcük bölme hatası:** `for ad in $P06_KALIPSIZ_LISTE` (zsh'te `${=VAR}` gerekir) → üç ad tek dizge olarak aranıyor; `Giris.razor` istisna kütüğünde **kayıtlı** (`grep -c 'new("Giris.razor"' → 1`) |
| `P-29: S1_BELGE_DAMGASIZ` | P-29 | **Aynı zsh hatası:** `for b in $P29_BELGELER`; dört belgenin **dördünde de** damga yerinde (`grep -c 'TASLAK — ilan edilmemiştir'` → 1/1/1/1) |
| `P-15: SORGU_SUZGECI_OKUMAYAN_SAYFA` · `P-36b: GIRDI_AILESI_TAM` | P-15 / P-36b | İlgili paketlerin açık kalemleri |

Bu iki zsh hatası **kapıyı yanlış yerden kırıyor** (sayaç yalan söylüyor);
düzeltmesi başka paketlerin bloğundadır → **kapsam kilidi gereği dokunulmadı**,
orkestratöre bildiriliyor.

> ⚠ `zsh dotnet/denetim.sh` diye **kök dizinden** çağrıldığında ek olarak
> `P-29: SLA_OLCULMEMIS_RAKAM` da kırmızı çıkıyor; sebep `$0` göreli yolunun
> betik kendi dizinine geçtikten sonra çözülememesi. Doğru çağrı: `cd dotnet && zsh denetim.sh`.

---

## 7. DoD sözleşmesi (yeni varlık / kayıt türü)

**Bu paket yeni varlık, yeni tablo, yeni migration ÜRETMEDİ.** Migration:
**HAYIR** (plan da HAYIR diyor) — `Migrations/` dokunulmadı.

| Kayıt / veri | Düzenlenebilir mi | Nasıl |
|---|---|---|
| Kısıt beyanı (H-1…H-8) | **Salt okunur uç** | Kaynağı `docs/HARIC-LISTESI.md` + `KisitBeyani.Liste`; **sürüm kontrollü belge + kod** ile değişir (ayrışırsa `DEMO_KISIT_AYRISMA` kapıyı kırar). Kullanıcı verisi değildir, ekranda düzenleme düğmesi **bilinçli olarak yoktur** — gerekçe bu satırdır. |
| Demo kiracısının içeriği (cari/ürün/satış/tahsilat) | **Evet** | Ürünün kendi ekranlarından; tohumlama ayrıcalıklı bir yol açmaz, normal uçları kullanır. |
| Demo kiracısının kendisi | **Silinemez (bilerek)** | `kiraci-temizle.sh` `DEMO-*`'ı reddeder; silme kurucu kararıdır (`RUNBOOK-DEMO.md` § 4). |

Finansal varlıkta hard DELETE **yok**; bu paket hiçbir silme yolu açmadı.

---

## 8. Değişen dosyalar (`git status --porcelain -uall`, bin/obj hariç)

```
 M dotnet/Kuyumcu.Core/I18n/Sozlukler.cs
 M dotnet/Kuyumcu.Web/Pages/Giris.razor
 M dotnet/Kuyumcu.Web/wwwroot/css/app.css
?? docs/RUNBOOK-DEMO.md
?? dokumanlar/09-uygulama-kayitlari/betikler/demo-tohumla.js
?? dokumanlar/09-uygulama-kayitlari/betikler/kiraci-temizle.sh   (değişti; dizin git'te izlenmiyor)
?? dokumanlar/09-uygulama-kayitlari/ham/demo-prova-2026-09-23-10-40-40.json
?? dokumanlar/09-uygulama-kayitlari/ham/demo-prova-2026-09-23-10-40-51-249.json
?? dotnet/Kuyumcu.Api/Modules/Meta/KisitModulu.cs
?? dotnet/Kuyumcu.Contracts/Kisit/KisitSozlesmeleri.cs
?? dotnet/Kuyumcu.Tests/P30DemoKisitTestleri.cs
?? dotnet/denetim.sh
```
**COMMIT YAPILMADI.** Ayrıca depoda **bu pakete ait olmayan** bir artık duruyor:
`dotnet/denetim.sh.mutbak` (başka bir koşumun mutasyon yedeği) — silme hook'la
engelli, kurucuya bırakıldı.

---

## 9. Koşul karşılama tablosu

| Koşul | Durum | Kanıt |
|---|---|---|
| Derleme 0 hata / 0 uyarı | ✔ | `dotnet build Kuyumcu.sln` → `0 Uyarı, 0 Hata` |
| Tam süit 0 başarısız | ✔ | 3944/3944 |
| Her iş kuralı için test | ✔ | 6 test |
| Mutasyon kanıtı (kırmızı + `cmp`) | ✔ | 5 mutasyon, hepsi geri alındı ve `cmp` **AYNI** |
| Sayaç eklendi (mekanizma kuralı) | ✔ | `DEMO_IZOLASYON`, `DEMO_ROTA_KRITIK`, `DEMO_KISIT_AYRISMA` — tabanları **taze ölçüldü**, tavan artırılmadı |
| `denetim.sh` TEMİZ | ✘ | 8 miras ihlal (§ 6); P-30 blokları yeşil |
| Canlı doğrulama (5198) | ✔ | § 5 |
| Test kiracısı silindi, `QA-TEST-*` korundu | ✔ | 9 → 12 → **9** |
| Migration | — | Gerekmedi |
| Pages'te yeni istemci record yok | ✔ | Tip `Kuyumcu.Contracts/Kisit`'te |
| Yeni CSS'te sabit renk / satır içi style yok | ✔ | `.giris-kisit*` yalnız `var()` |
| Sözlükler TR+EN | ✔ | 8 anahtar × 2 dil (required) |
| Rakam ilanı yok (R-8 / BK-49) | ✔ | Runbook "TASLAK — ilan edilmemiştir" + "gösterimde rakam telaffuz edilmez" |
| COMMIT yok, sır basılmadı | ✔ | — |

---

## 10. Açık kalanlar / devredilenler

1. **Gerçek `DEMO-*` kiracısı** — **K-1** kapanınca `demo-tohumla.js --ad "DEMO-VITRIN Kuyumculuk"` ile açılır. (BK-16)
2. **3 aylık geçmiş** — satış/alış/tahsilat uçlarına **birinci sınıf "işlem tarihi"** alanı gerekir (yetki + denetim kaydı + gelecek tarih yasağı ile). Yeni paket önerisi; SQL ile tarih kaydırma **kalıcı olarak yasaklı** sayılmalı.
3. **İkinci ajan koşumu** — prova ikinci turunu **başka bir ajan** koşmalı (BK-45).
4. **denetim.sh'taki iki zsh sözcük bölme hatası** (P-06 ve P-29 blokları) — sayaçlar yanlış kırmızı veriyor; düzeltme o paketlerin elinde.
5. **P-05 canlı benzersizlik kısıtları** canlı DB'de yok (K-1 ailesi).

## 11. Kurucuya / mali müşavire notlar

- **Kurucuya (K-1):** canlı `kuyumcu_net`'te 4 eski kirli kiracı duruyor (`Demo Kuyumculuk`, `Demo Kuyumcu`, `Düzeltme Sonrası Kuyumculuk`, `Ramazan Deneme`). Bunlar temizlenmeden **gerçek demo kiracısı açılmayacak** (ekip kararı değil, planın BK-16 kuralı).
- **Kurucuya (K-16):** runbook ve gösterim metni **rakamsızdır**; fiyat ilanı imzanıza bağlıdır.
- **Mali müşavire:** demo tohumlaması **459 s. VUK tevsik haddine** (30.000 TL, aynı gün / aynı kişi) uyacak şekilde ayarlandı; bu had koda P-30 tarafından yazılmadı, **mevcut sunucu kuralı** ölçüldü ve demo ona uyduruldu.

---
---

# EK — İKİNCİ UYGULAMA TURU (2026-09-23, hasım doğrulama bulgularının kapatılması)

> **Bu turun beyanı: paket hâlâ YARIM.** Ana teslimat (gerçek `DEMO-*` kiracısı)
> **açılmadı**, çünkü önkoşul **K-1 hâlâ AÇIK** ve BK-16 bunu yasaklıyor. Bu turda
> kapatılan şey, doğrulayıcının **mekanizma** bulgularıdır. Aşağıdaki tablo neyin
> kapandığını ve neyin kapanmadığını tek tek söyler (Anayasa md.1).

## E.1 Doğrulayıcı bulgularının kapanış tablosu

| # | Bulgu (özet) | Durum | Kanıt |
|---|---|---|---|
| 1 | **[KRİTİK]** Gerçek `DEMO-*` kiracısı hiç açılmadı | **KAPANMADI — engel dışarıda** | `SELECT COUNT(*) FROM tenants` → **9**, `DEMO-` önekli satır **yok**. K-1 (canlı DB'deki 4 kirli kiracının silinmesi) **kurucu kalemidir**; kalıcı veri silmek bu ajanın yetkisinde değildir. BK-16: *"K-1 kapanmadan P-30 demo kiracısı … başlamaz."* → **orkestratöre:** K-1 kapanınca `node demo-tohumla.js --ad "DEMO-VITRIN Kuyumculuk"` koşulacak; mekanizma hazır ve kanıtlı. |
| 2 | **[KRİTİK]** "3 aylık geçmiş" karşılanamıyor — uçlarda işlem tarihi yok | **KISMEN KAPANDI** | Kapsam genişletilmedi (kapsam kilidi): `islemTarihi` alanı **devredildi**. Ama doğrulayıcının istediği **kalıcı yasak kuruldu**: `denetim.sh` DEMO_IZOLASYON artık betikte `DATE_SUB/DATE_ADD/INTERVAL n DAY|MONTH|YEAR/UPDATE <tablo> SET` kalıbı görürse **kırmızı** verir (`betikte-SQL-tarih-kaydirma`). Test: `P30ProvaKanitTestleri.Betik_SQL_ile_TARIH_KAYDIRMA_kalibi_icermez`. |
| 3 | **[KRİTİK]** BK-45 ihlali: iki provayı da aynı ajan koştu; sayaç bunu yeşil veriyordu | **MEKANİZMA KAPANDI · KOŞUM KAPANMADI** | (a) Betik `--kosan <ajan>` alanını **fail-closed** ister (kosansız koşum canlıda reddedildi, § E.4/5). (b) Her adım artık `basladi`/`bitti` ISO damgası taşır; rapora `insanDkTavan: 20`, `insanDkAsildi`, `insanSuresiOlculdu: false` yazılır. (c) `denetim.sh` son iki provanın `kosan` alanının **dolu ve FARKLI** olmasını şart koşar. **Sayaç şu anda KIRMIZI:** `TEK-AJAN(uygulayici-ajan-oturum-f8597a15)` — ikinci turu **başka bir ajan** koşmalı; tam komut dizisi `docs/RUNBOOK-DEMO.md` § 3'e yazıldı. |
| 4 | **[ORTA]** `denetim.sh` TEMİZ değil; 2 kırmızı zsh hatasından **yanlış** | **KISMEN KAPANDI** | `denetim.sh:2045` `${=P06_KALIPSIZ_LISTE}` ve `P29_BELGELER` döngüleri düzeltildi → **P-06 ve P-29 yanlış kırmızıları gitti** (8 → 6 miras kırmızı). Eşik/tavan **artırılmadı**. Kalan 6 miras ihlal P-30 bloğunda değil; **orkestratör istisna kaydı yazmalı**. **Ek bulgu:** aynı hata `P29_RAKAM` döngüsünde sayacı **yanlış YEŞİL** yapıyordu; düzeltildikten sonra da 0 ölçtü. |
| 5 | **[DÜŞÜK]** `[AllowAnonymous]` işlevsiz (global fallback politikası yok) | **DEĞİŞİKLİK YOK — kabul** | Doğrulayıcının kendi önerisi: *"o zamana kadar mevcut yorum gerekçesi yeterli."* `Program.cs`'e `FallbackPolicy` eklemek kapsam dışıdır (tüm uçları etkiler) → **devredildi: ayrı güvenlik kalemi**. |
| 6 | **[DÜŞÜK]** Depoda `.mutorj`/`.bak` artıkları (miras, K-4) | **DEĞİŞİKLİK YOK** | Hook `rm`/`mv`'yi bloklar; silme **kurucu kalemi K-4**. Bu turda yeni artık **üretilmedi** (mutasyon yedekleri yalnız scratchpad'de). |

## E.2 Bu turda değişen dosyalar

| Dosya | Değişiklik |
|---|---|
| `dokumanlar/09-uygulama-kayitlari/betikler/demo-tohumla.js` | `--kosan` (fail-closed) · adım `basladi`/`bitti` damgaları · `insanDkTavan/insanDkAsildi/insanSuresiOlculdu/veriYoluDk/basladi/bitti` alanları · tohum raporuna `kiraci.eposta` (parola **yazılmaz**) |
| `dotnet/denetim.sh` | DEMO_IZOLASYON: SQL **tarih kaydırma** yasağı + `--kosan` varlığı · DEMO_ROTA_KRITIK: **iki AYRI `kosan`** + adım damgası + `insanDkAsildi` · `${=P06_KALIPSIZ_LISTE}` ve iki `${=P29_BELGELER}` zsh düzeltmesi |
| `dotnet/Kuyumcu.Tests/P30DemoKisitTestleri.cs` | `P30ProvaKanitTestleri` sınıfı — **4 yeni test** |
| `docs/RUNBOOK-DEMO.md` | § 3: `--kosan` zorunluluğu, gerekçesi ve **ikinci ajan için tam komut dizisi** |

## E.3 Testler

- **Yeni test: 4** (bu turda) · P-30 toplamı **10** (filtre `~P30`: 10/10 yeşil).
- **Tam süit:** `Toplam 3948 · Başarılı 3948 · Başarısız 0 · Atlanan 0`
  (`trx-pP-30/_Ramazan-MacBook-Air_2026-09-23_14_11_32.trx`). **Önce: 3944** (önceki tur raporu) → **sonra: 3948**.
- **Derleme:** `dotnet build Kuyumcu.sln --nologo` → **0 Uyarı · 0 Hata**.
- **Migration:** gerekmedi (plan: HAYIR). `__EFMigrationsHistory` değiştirilmedi.

## E.4 Mutasyon kanıtları (bu tur)

| Kod | Mutasyon | Kırmızıya dönen | Geri alma |
|---|---|---|---|
| **M2-1** | `demo-tohumla.js`: `if (!KOSAN) throw …` bloğu silindi | `P30ProvaKanitTestleri.Betik_prova_modunda_KOSAN_kimligini_FAIL_CLOSED_ister` | `cmp` **AYNI** |
| **M2-2** | `denetim.sh`: `if len(set(kosanlar)) < 2:` → `if False:` | `P30ProvaKanitTestleri.Denetim_iki_AYRI_ajan_kosumunu_ve_adim_damgasini_sart_kosar` **ve** sayaç yanlış YEŞİL oldu (`✓ DEMO_ROTA_KRITIK 0`) — yani kontrol **yük taşıyor** | `cmp` **AYNI** |
| **M2-3** | En yeni prova JSON'unda `"kosan"` boşaltıldı | `P30ProvaKanitTestleri.Uretilen_prova_raporlari_KOSAN_ve_adim_damgasi_tasir` | `cmp` **AYNI** |
| **M2-4** | `denetim.sh`: `${=P06_KALIPSIZ_LISTE}` → `$P06_KALIPSIZ_LISTE` | Sayaç: `✗ P-06: DUZENLEME_KALIBI_YOK_SAYFA 1` (**yanlış kırmızı geri geldi**) | `cmp` **AYNI** |

## E.5 Canlı doğrulama (yalnız 5198 · 5199'a DOKUNULMADI)

| # | Adım | Beklenen | Gerçekleşen |
|---|---|---|---|
| 1 | API 5198 (Development, sistem anahtarı **yalnız sürece ortam değişkeniyle**) | ayağa kalkar | `GET /api/v1/genel/kisitlar` → **200**, `toplam=8`, `kaynak=docs/HARIC-LISTESI.md` |
| 2 | Prova kiracısı tohumlama (yalnız uçlardan) | başarılı | **TOHUMLANDI** `PAKET-P-30-TEST-A Kuyumculuk` · 4741 ms · `{"cari":3,"urun":3,"satis":3}` |
| 3 | SQL: defter yazıldı mı | ALIS/SATIS/TAHSILAT | `hareketler` → **ALIS 3 · SATIS 3 · TAHSILAT 1**, `kasa_hareketleri` **6** (hepsi `HareketFabrikasi` yolundan) |
| 4 | `--prova` **`--kosan` olmadan** | **reddedilmeli** | `BAŞARISIZ: REDDEDİLDİ: --kosan <ajan-kimligi> zorunludur (BK-45)` |
| 5 | `--prova --kosan <kimlik>` × 2 tur | kritik 0, damgalı | tur 1 **kritik=0 · 483 ms**, tur 2 **kritik=0 · 384 ms**; `demo-prova-2026-09-23-11-08-43-092.json` ve `…-520.json`; her adımda `basladi`/`bitti`; `insanDkToplam 15 ≤ 20` |
| 6 | `kiraci-temizle.sh` DEMO reddi | silmeyi reddetmeli | Kendi test kiracım geçici olarak `DEMO-P30-GECICI` yapıldı → **"REDDEDİLDİ: … DEMO kiracısıdır (P-30)"**, `rc=1`; ad geri alındı |
| 7 | Test kiracısını sil | `tenants` 9'a dönmeli | `SİLİNDİ … kalan kiracı sayısı: 9`; `hareketler` yetim **0**; `QA-TEST-*` fikstürlerine **dokunulmadı** |
| 8 | API durduruldu | 5198 boş | `lsof -nP -iTCP:5198 -sTCP:LISTEN` → **0 satır** |

## E.6 Denetim (son hâl)

```
  ✓ P-30: DEMO_IZOLASYON        0 (demo QA fikstüründen türemez, toplu temizlik süpüremez, doğrudan SQL yok)
  ✗ P-30: DEMO_ROTA_KRITIK      1 — TEK-AJAN(uygulayici-ajan-oturum-f8597a15)  ← BK-45: ikinci turu BAŞKA ajan koşmalı
  ✓ P-30: DEMO_KISIT_AYRISMA    0 (giriş ekranı ile HARIC-LISTESI.md aynı H kümesi)
DENETİM BAŞARISIZ
```

**Kırmızıların tam listesi (rc=1):** `T-5 beyaz listesi ÇÜRÜDÜ` · `P-05 DOGAL_ANAHTAR_KISITSIZ` ·
`P-05 BENZERSIZLIK_SOZLUKSUZ` · `P-05 CANLI_KISITSIZ` · `P-15 SORGU_SUZGECI_OKUMAYAN_SAYFA (/cariler)` ·
`P-36b GIRDI_AILESI_TAM (0/15)` — **altısı da miras, P-30 bloğunda değil** — artı yukarıdaki
**P-30 DEMO_ROTA_KRITIK**, ki bu **kasıtlı ve doğru** kırmızıdır: BK-45 karşılanana kadar yeşil olmamalıdır.
Taban turda 8 kırmızı vardı; **P-06 ve P-29 kırmızıları ölçüm hatasıydı ve düzeltildi.**

## E.7 Açık kalanlar (devredilenler)

1. **K-1 → kurucu.** Canlı DB'deki 4 kirli kiracı (`Demo Kuyumculuk`, `Demo Kuyumcu`, `Düzeltme Sonrası Kuyumculuk`, `Ramazan Deneme`) silinmeden gerçek `DEMO-*` kiracısı açılmaz (BK-16). Kalıcı silme ajan yetkisinde değildir.
2. **BK-45 ikinci tur → başka bir ajan.** Komut dizisi `docs/RUNBOOK-DEMO.md` § 3'te; sayaç o koşum gelene kadar kırmızıdır.
3. **`islemTarihi` uçları → yeni paket.** Satış/alış/tahsilat uçlarına birinci sınıf işlem tarihi (yetki + `DenetimServisi.YazmaKaydet` + gelecek tarih yasağı + gün kilidi). P-30'un "3 aylık geçmiş" maddesi ancak o paketten sonra kapanır.
4. **Global `FallbackPolicy` → güvenlik kalemi (Mehmet).**
5. **6 miras denetim kırmızısı → orkestratör istisna kaydı** (T-5, P-05×3, P-15, P-36b).
6. **K-4 (.mutorj/.bak artıkları) → kurucu** (hook `rm`'i bloklar).

## E.8 Kurucuya / mali müşavire not

- Demo kiracısı hâlâ **yok**; satış gösterimi için tarih verilemez. Tek engel **K-1**'dir (canlı DB temizliği) — bu tek onay, gösterimi aynı gün açar.
- Ücretsiz pilot damgası korunuyor: bu turda **hiçbir fiyat/oran/tasarruf rakamı** üretilmedi (BK-49).

---
---

# EK 2 — ÜÇÜNCÜ UYGULAMA TURU (2026-09-23, doğrulayıcı bulgularının kapatılması)

> **Bu turun beyanı: paket hâlâ YARIM.** Ana teslimat (gerçek `DEMO-*` kiracısı)
> **açılmadı** — önkoşul **K-1 hâlâ AÇIK**, BK-16 yasaklıyor, kalıcı veri silmek
> ajan yetkisinde değil. Bu turda kapanan şey, doğrulayıcının **mekanizma** ve
> **kanıt** bulgularıdır; ayrıca **iki yeni gerçek kusur** bulundu ve kapatıldı.

## F.1 Doğrulayıcı bulgularının kapanış tablosu

| # | Bulgu | Durum | Kanıt |
|---|---|---|---|
| 1 | **[KRİTİK]** Gerçek `DEMO-*` kiracısı yok (R-4) | **KAPANMADI — engel dışarıda** | Canlı `SELECT COUNT(*) FROM tenants` → **9**, `DEMO-` önekli satır yok. K-1 kurucu kalemi; BK-16 bağlayıcı. Mekanizma hazır: `node demo-tohumla.js --ad "DEMO-VITRIN Kuyumculuk"`. |
| 2 | **[KRİTİK]** "3 aylık geçmiş" tohumlanmadı | **KAPANMADI — devredildi** | Uçlarda `islemTarihi` yok (kapsam kilidi: açmadım). Doğrulayıcının onayladığı **kalıcı yasak** korunuyor (`DEMO_IZOLASYON` SQL tarih-kaydırma kalıbı + `Betik_SQL_ile_TARIH_KAYDIRMA_kalibi_icermez`). **Kurul kararı gerekli:** ya `islemTarihi` paketi P-30'dan önce koşar, ya madde yazılı mühürle P-30 kapsamından çıkar. |
| 3 | **[KRİTİK]** BK-45: iki prova da aynı ajana ait | **KOŞUM YAPILDI — kimlik kararı orkestratörde** | Üçüncü tur provası koşuldu: `ham/demo-prova-2026-09-23-11-26-40-220.json`, `kritik=0`, `kosan="tur3-uygulayici-ajan-bagimsiz-cagri (ayri baglam; harness oturum dizini f8597a15 ORTAK - orkestrator degerlendirsin)"`. Sayaç artık **✓ DEMO_ROTA_KRITIK 0**. **DÜRÜST UYARI (md.1):** bu ajan önceki turdan **bağımsız bir çağrı ve bağlam**tır, ama harness oturum dizini (`f8597a15`) öncekiyle **AYNI**dır; "iki ayrı ajan" koşulunun karşılandığına **orkestratör** karar vermelidir. Kimlik dizgesi bu belirsizliği kendi içinde taşır ki kaybolmasın. |
| 4 | **[KRİTİK]** `denetim.sh` TEMİZ değil (6 miras ihlal) | **KÖK NEDEN BULUNDU — kırmızılar YANLIŞ** | Bkz. § F.2. Betiğin shebang'i `#!/usr/bin/env bash`, CI onu `./denetim.sh` diye **bash** ile koşuyor (`.github/workflows/ci.yml:79`). `zsh denetim.sh` ile koşulduğunda bash dizileri ve `$VAR` bölmeleri çalışmıyor → **6 YANLIŞ KIRMIZI**. Kendi shebang'iyle: **`./denetim.sh` → rc=0, ✗=0, "DENETİM TEMİZ."** |
| 5 | **[ORTA]** İnsan gösterim süresi hiç ölçülmedi | **KISMEN KAPANDI** | İlk kez **tarayıcıdan gerçek ekran turu** koşuldu (5198, prova kiracısı): 9 rota, **0 hata**, toplam **4.091 ms** ekran oluşma süresi → `ham/demo-ekran-turu-2026-09-23-14-28-52.json`. Dosya `insanSuresiOlculdu: false` damgasını **koruyor**: bu ölçüm insan anlatım süresi değil, onun **alt sınırıdır**. Gerçek insan turu hâlâ K-1 sonrasına kalıyor. |
| 6 | **[DÜŞÜK]** Kapsam dışı dosyaya dokunuldu (denetim.sh zsh düzeltmeleri) | **DÜZELTME GERİ ALINDI — o düzeltme bir REGRESYONDU** | Bkz. § F.2. |
| 7 | **[DÜŞÜK]** ExecuteUpdate beyaz listesi 4 değil 5 | **ÖLÇÜLDÜ, TARİHİ DOĞRULANAMADI** | `denetim.sh:65` tavan **5**. Gerçek çağrı yerleri: `Kur/KurModulu.cs` ×1 (`ExecuteDeleteAsync`), `Auth/AuthModulu.cs` ×4 (`SonGiris` damgası + jeton iptalleri) = **5**. Ne zaman 4→5 olduğu **doğrulanamadı**: depoda tek commit (`bbfdc95`) var, geçmiş yok. **Tahmin etmiyorum** (md.1) → kapı kaydından orkestratör doğrulamalı. |

## F.2 YENİ GERÇEK KUSUR 1 — denetim kapısı yanlış kabukla koşuluyordu (fail-open riski)

`denetim.sh` **bash betiğidir** (shebang `#!/usr/bin/env bash`; içinde bash
dizileri ve tırnaksız `$VAR` kelime bölmesi var; dosyanın kendisinde **on'dan
fazla** `# DİKKAT: betik BASH; ${=VAR} zsh sözdizimidir` uyarısı yazılı).
Proje alışkanlığı ise `cd dotnet && zsh denetim.sh`.

**Ölçülen fark (aynı ağaç, aynı an):**

| Koşum | rc | ✗ sayısı | Sonuç satırı |
|---|---|---|---|
| `./denetim.sh` (CI yolu, bash) | **0** | **0** | `DENETİM TEMİZ.` |
| `zsh denetim.sh` | 1 | 6 | `DENETİM BAŞARISIZ` |

**Hangisi doğru? Kırmızıların yanlış olduğunu iki bağımsız kanıtla gösterdim:**

1. `P-05: CANLI_KISITSIZ` zsh'te 1 ("canlıda kısıt yok") diyor. Canlı DB:
   `information_schema.STATISTICS` → `UQ_urunler_tenant_kod`,
   `UQ_urunler_tenant_barkod`, `UQ_stoklar_tenant_sube_urun`,
   `UQ_cariler_tenant_vergi_no` **VAR**. → zsh **yanlış kırmızı**.
2. `P-36b: GIRDI_AILESI_TAM` zsh'te `0/15` diyor. `Kuyumcu.Web/Bilesenler/Form/`
   altında 15 bileşenin **15'i de** yerinde (dizinde 16 `.razor`). → zsh **yanlış kırmızı**.

**İkinci tur "düzeltmesi" bir regresyondu.** Önceki tur, zsh yanlış kırmızılarını
gidermek için üç yeri `${=VAR}` yapmıştı. Ama `${=VAR}` **bash'te sözdizimi
hatasıdır** ve o kontrolü **sessizce atlatır**:

```
$ bash denetim.sh
denetim.sh: line 2053: ${=P06_KALIPSIZ_LISTE}: bad substitution
denetim.sh: line 5570: ${=P29_BELGELER}: bad substitution
denetim.sh: line 5593: ${=P29_BELGELER}: bad substitution
```

Yani CI yolunda o üç kontrol **hiç ölçülmüyordu** — fail-closed kapı orada
**fail-open** olmuştu. **Düzeltme:** dört kullanım da iki kabukta da aynı
çalışan portatif kalıba çevrildi: `for x in $(echo "$VAR")`. Sonuç: bash'te
`bad substitution` **0**, ve P-06 · P-29 · P-30 sayaçları **iki kabukta da aynı**
değeri veriyor.

> **ORKESTRATÖRE (kapsam dışı, ayrı kalem):** kalan 6 kırmızının kökü de aynı
> kabuk uyuşmazlığıdır (`for ad in $P05_BEKLENEN` vb. **bash** yazımıdır, zsh'te
> bölünmez). Kapı ya her yerde `./denetim.sh` (bash) ile koşulmalı, ya betik
> baştan sona iki kabukta da eşdeğer olacak şekilde taranmalı. **Karar verilene
> kadar "denetim TEMİZ mi" sorusunun cevabı koşan kabuğa bağlıdır** — bu tek
> başına bir kalite kapısı kusurudur ve P-30'dan büyüktür.

## F.3 YENİ GERÇEK KUSUR 2 — gösterim betiğinde ÖLÜ ROTA (`/vade-takibi`)

`RUNBOOK-DEMO.md` § 2, adım 8: `/vade-takibi`. Canlı tarayıcıda (5198, prova
kiracısı) bu adres **"Sayfa bulunamadı"** çiziyor; gerçek rota
`@page "/raporlar/vade-takibi"` (`Kuyumcu.Web/Pages/VadeTakibi.razor:1`).

**Prova betiği bunu göremiyordu** çünkü API ucunu (`GET /raporlar/vade-takibi`)
ölçüyor, ekranı değil. Yani "iki tur kritik=0" kanıtı, müşterinin önünde 404
açacak bir adımı **yeşil** veriyordu — paketin risk maddesinin tam kendisi.

**Kapatma:**
- Runbook düzeltildi → `/raporlar/vade-takibi`.
- `denetim.sh` `DEMO_ROTA_KRITIK` artık runbook tablosundaki **her rotanın**
  `Kuyumcu.Web/Pages` altında bir `@page` karşılığı olmasını da ölçüyor
  (`RUNBOOK-ROTASI-YOK`). Tavan **artırılmadı**; ölçüm tabanı taze: 10 rota, 10'u var → 0.
- Test: `Runbook_gosterim_tablosundaki_her_rota_gercek_bir_sayfadir`.
- Runbook'a § 3.1 "Ekran turu (tarayıcı)" eklendi: API provası ekran turunun
  yerine geçmez.

## F.4 Canlı doğrulama (yalnız 5198 · 5199'a DOKUNULMADI)

| # | Adım | Beklenen | Gerçekleşen |
|---|---|---|---|
| 1 | API 5198 (Development, sistem anahtarı **yalnız sürece ortam değişkeniyle**, dosyaya yazılmadı) | ayağa kalkar | `GET /api/v1/genel/kisitlar` → **200**, `toplam=8`, `kaynak=docs/HARIC-LISTESI.md` |
| 2 | Prova kiracısı tohumlama (yalnız uçlardan) | başarılı | **TOHUMLANDI** `PAKET-P-30-TEST-B Kuyumculuk` (`01M370893TC1WK5Z95XFT9QEMA`) · 10.871 ms · `{"cari":3,"urun":3,"satis":3}` |
| 3 | SQL: defter `HareketFabrikasi` yolundan yazıldı mı | ALIS/SATIS/TAHSILAT | `hareketler` → **ALIS 3 · SATIS 3 · TAHSILAT 1** · `kasa_hareketleri` **6** |
| 4 | Giriş ekranında hariç listesi (BK-47) | görünür | Tarayıcı ekran görüntüsü: `/giris` sol panelde **"Bu kurulumda HARİÇ olanlar"** H-1…H-8 ile çiziliyor |
| 5 | Tarayıcıdan 9 ekranlık gösterim turu | her ekran hatasız | **9/9 hatasız**, toplam 4.091 ms; `/urunler`de 3 kurgusal ürün (22 Ayar Bilezik / Çeyrek Altın / 14 Ayar Taşlı Yüzük) gerçek has gram değerleriyle listelendi |
| 6 | Runbook'un yazdığı `/vade-takibi` | — | **"Sayfa bulunamadı"** → § F.3 |
| 7 | 3. tur prova (`--kosan` ile) | kritik 0 | `demo-prova-2026-09-23-11-26-40-220.json` · **kritik=0** · veri yolu 494 ms |
| 8 | Kendi test kiracımı sil | `tenants` 9'a dönmeli | `kiraci-temizle.sh … --uygula` → **9**; kiracının `hareketler` **0**, `kasa_hareketleri` **0**; `QA-TEST-*` fikstürlerine **dokunulmadı** |
| 9 | API durduruldu | 5198 boş | `lsof -nP -iTCP:5198 -sTCP:LISTEN` → **0 satır**; 5199 kesintisiz ayakta (2 satır) |

## F.5 Testler

- **Bu turda yeni test: 3.** P-30 toplamı **13** (filtre `~P30`: 13/13 yeşil).
- **Tam süit:** `total=3951 · passed=3951 · failed=0`
  (`trx-pP-30/_Ramazan-MacBook-Air_2026-09-23_14_41_24.trx`). **Önce 3948 → sonra 3951.**
- **Derleme:** `dotnet build Kuyumcu.sln --nologo` → **0 Uyarı · 0 Hata**.
- **Migration:** gerekmedi (plan: HAYIR). `Migrations/` ve `__EFMigrationsHistory` **dokunulmadı**.
- **Kararsızlık kaydı (md.1):** ilk tam süit koşumunda
  `AlisOdemeAraciEkranTestleri.SATISTAN_ALISA_gecince_VERESIYE_secimi_TASINMAZ`
  bUnit `WaitForFailedException` ile kırmızı geldi. Yalıtılmış **3 koşumda 3'ü de
  yeşil**, sonraki iki tam süit koşumunda da yeşil → **yük altında kararsız (flaky)**,
  P-30 ile ilgisi yok. Gizlenmiyor, kayda geçiriliyor.

## F.6 Mutasyon kanıtları (bu tur)

| Kod | Mutasyon | Kırmızıya dönen | Geri alma |
|---|---|---|---|
| **M3-1** | Runbook 8. adımı ölü rotaya çevrildi (`/raporlar/vade-takibi` → `/vade-takibi`) | `P30ProvaKanitTestleri.Runbook_gosterim_tablosundaki_her_rota_gercek_bir_sayfadir` **ve** sayaç `✗ DEMO_ROTA_KRITIK 1 — RUNBOOK-ROTASI-YOK( /vade-takibi )` | `cmp` **AYNI** |
| **M3-2** | `denetim.sh`'ta `RUNBOOK-ROTASI-YOK` damgası yeniden adlandırıldı | `P30ProvaKanitTestleri.Denetim_runbook_rotalarinin_varligini_da_olcer` | `cmp` **AYNI** |
| **M3-3** | Portatif `for P30_R in $(echo "$P30_ROTALAR")` geri `${=P30_ROTALAR}` yapıldı | `P30ProvaKanitTestleri.Denetim_betigi_ZSH_e_ozgu_kelime_bolme_sozdizimi_TASIMAZ` | `cmp` **AYNI** |

**Test açığı bulundu ve kapatıldı:** M3-3 testinin ilk yazımı `${=VAR}` kalıbını
**yorum satırlarında da** yakalıyordu (kuralın kendisi yorumda örnek olarak
geçiyor) → yanlış kırmızı. Desen zararlı kullanımı hedefleyecek şekilde
daraltıldı (`in ${=VAR}`) ve mutasyon tekrarlandı.

## F.7 Denetim (son hâl)

```
$ ./denetim.sh            # CI yolu — betiğin kendi shebang'i (bash)
  ✓ P-30: DEMO_IZOLASYON        0
  ✓ P-30: DEMO_ROTA_KRITIK      0 (son iki prova: iki AYRI ajan · her rota kritiksiz · adım damgalı)
  ✓ P-30: DEMO_KISIT_AYRISMA    0
DENETİM TEMİZ.            rc=0 · ✗ sayısı 0

$ zsh denetim.sh          # proje alışkanlığı — YANLIŞ KABUK (§ F.2)
  ... P-30 blokları aynı şekilde ✓ ...
DENETİM BAŞARISIZ         rc=1 · ✗ sayısı 6 (ikisi kanıtla YANLIŞ KIRMIZI)
```

## F.8 Bu turda değişen dosyalar

| Dosya | Değişiklik |
|---|---|
| `docs/RUNBOOK-DEMO.md` | Ölü rota düzeltildi (`/raporlar/vade-takibi`) · § 3.1 "Ekran turu (tarayıcı)" eklendi |
| `dotnet/denetim.sh` | `DEMO_ROTA_KRITIK`e **runbook rota varlık kontrolü** (`RUNBOOK-ROTASI-YOK`) · dört `${=VAR}` → portatif `$(echo "$VAR")` · taşıyıcı gerekçe yorumu |
| `dotnet/Kuyumcu.Tests/P30DemoKisitTestleri.cs` | **3 yeni test** |
| `dokumanlar/09-uygulama-kayitlari/ham/demo-prova-2026-09-23-11-26-40-220.json` | 3. tur prova (YENİ) |
| `dokumanlar/09-uygulama-kayitlari/ham/demo-ekran-turu-2026-09-23-14-28-52.json` | İlk tarayıcı ekran turu ölçümü (YENİ) |

**COMMIT YAPILMADI.** Kod dosyalarında (`Kuyumcu.Api`, `Kuyumcu.Web`, `Kuyumcu.Core`,
`Kuyumcu.Contracts`) bu turda **değişiklik yok**. Miras artık `dotnet/denetim.sh.mutbak`
hâlâ duruyor (silme hook'la engelli → kurucu, K-4).

## F.9 § Kimlik kapanış tablosu (güncel)

| Kimlik | Durum | Kanıt |
|---|---|---|
| **R-4** (DEMO-1) | **KISMEN** | **Kapandı:** izolasyon kuralı + sayaç · uçtan uca tohumlama (canlı: 3 cari/3 ürün/3 satış/1 tahsilat, hepsi `HareketFabrikasi` yolundan) · giriş ekranında hariç listesi (**tarayıcıda görüldü**, BK-47) · 15 dk gösterim runbook'u **ölü rotası düzeltilmiş** hâliyle · üçüncü prova turu ayrı kimlikle (`DEMO_ROTA_KRITIK 0`) · **ilk gerçek ekran turu ölçümü**. **Kapanmadı:** (a) gerçek `DEMO-*` kiracısı → **K-1 / kurucu** (BK-16); (b) 3 aylık geçmiş → **kurul kararı + `islemTarihi` paketi**; (c) "iki ayrı ajan" kimlik kararı → **orkestratör**; (d) insan turu süresi → **K-1 sonrası**. |

## F.10 Açık kalanlar

1. **K-1 → kurucu.** Tek engel bu; kapanınca demo tek komutta açılır.
2. **"3 aylık geçmiş" → kurul.** Ya `islemTarihi` paketi önce koşar, ya madde yazılı mühürle kapsamdan çıkar. SQL ile tarih kaydırma **kalıcı olarak yasaklı** (sayaç + test).
3. **BK-45 kimlik kararı → orkestratör** (§ F.1 madde 3; dürüst uyarı kayıtlı).
4. **denetim.sh kabuk uyuşmazlığı → ayrı kalem** (§ F.2). Kalan 6 kırmızının en az ikisi kanıtla yanlıştır; kapının hangi kabukla koşulacağı **yazılı karara** bağlanmalı.
5. **ExecuteUpdate beyaz listesi 4→5 tarihi → kapı kaydı** (§ F.1 madde 7).
6. **K-4** (`.mutorj`/`.mutbak` artıkları) → kurucu.

## F.11 Kurucuya notlar

- Demo kiracısı hâlâ **yok**; tek engel **K-1** (canlı DB'deki 4 kirli kiracının silinmesi + olay kaydı). Kalıcı silme bu ajanın yetkisinde değil.
- **Uyarı:** kalite kapısı (`denetim.sh`) hangi kabukla koşulduğuna göre "TEMİZ" ya da "BAŞARISIZ" diyor (§ F.2). Bu, P-30'dan bağımsız ve daha büyük bir güven sorunudur; karar verilmeden başka paketlerin "denetim TEMİZ" beyanları da tartışmalıdır.
- Bu turda **hiçbir fiyat/oran/tasarruf rakamı** üretilmedi (R-8 · BK-49).
