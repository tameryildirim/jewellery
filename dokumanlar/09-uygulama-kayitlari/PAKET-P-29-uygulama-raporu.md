# PAKET P-29 — S-1 TİCARİ PAKET + ÜRÜN ANLATIMI (BELGE HATTI) · uygulama raporu

**Tarih:** 2026-09-23 · **Dalga:** DALGA 7 · **Kimlikler:** R-3 · R-8 · D-8
**Rol:** Taşkın (backend) + Ertuğ (frontend) · belge hattı, kod hattından bağımsız
**Migration:** HAYIR (gerekmedi — tek satır şema değişikliği yok)

---

## § 0 — ÖNKOŞUL ÖLÇÜMÜ (P-28, varsayım yapılmadı)

| Beklenen çıktı | Diskte | Kanıt |
|---|---|---|
| P-28 uygulama raporu | **VAR** | `dokumanlar/09-uygulama-kayitlari/PAKET-P-28-uygulama-raporu.md` |
| Telemetri modülü | **VAR** | `dotnet/Kuyumcu.Api/Modules/Telemetri/` (7 dosya) |
| F-8 birim ekonomi belgesi | **VAR** (31.872 bayt) | `dokumanlar/02-mimari/F-8-BIRIM-EKONOMI-2026-09-16.md` |
| F-8'de ölçülmemiş satır | **2** (`OLCULMEDI`) | destek dakikası · kiracı başı kaynak tüketimi |

→ Önkoşul karşılandı. **Ama F-8'de 2 satır hâlâ ölçülmemiş** olduğu için
**FAIL-CLOSED 1 aktiftir**: bu paketin ürettiği hiçbir metinde rakamlı fiyat,
tasarruf oranı ya da kapasite iddiası yoktur (BK-49). Kapsam bu gerçeğe göre
daraltılmadı — plan zaten bunu emrediyordu.

---

## § 1 — YAPILAN İŞ

Dört belge yazıldı, `dotnet/denetim.sh`'a **dört cırcır sayaç** eklendi.

| Dosya | Durum | İçerik |
|---|---|---|
| `docs/HARIC-LISTESI.md` | **YENİ** | 1.0'da HARİÇ olan 8 kalem (H-1…H-8) + **GÖÇ KARARI** + kurucu kalemleri + değişiklik kuralı. **Kısıt beyanının TEK KAYNAĞI** (BK-26 / Köprü K-1). |
| `docs/SLA.md` | **YENİ** | Üç kutu: **TAAHHÜT EDİLEBİLİR** (7 kanıtlı satır) · **YAPILANDIRILMIŞ, ÖLÇÜLMEMİŞ** (4 satır) · **TAAHHÜT YOK** (5 satır). Kanıtsız hiçbir sayı yok. |
| `docs/HIZLI-BASLANGIC.md` | **YENİ** | Kurulumdan ilk satışa 8 adım + **canlı koşumdan çıkan 6 gerçek tuzak** + ölçüm tablosu (1 koşum yapıldı, 2 koşum açık). |
| `docs/URUN-ANLATIMI.md` | **YENİ (D-8)** | "Dükkânın 8 acı noktası → karşılığı", her satırın yanında **dosya:satır** kanıtı + R-8 konumlandırması + "ne söylemiyoruz" bölümü. |
| `dotnet/denetim.sh` | **DEĞİŞTİ** | `# >>> P-29` … `# <<< P-29` bloğu (satır 5483-5594), 4 sayaç. |
| `dokumanlar/09-uygulama-kayitlari/PAKET-P-29-uygulama-raporu.md` | **YENİ** | Bu belge. |
| `dokumanlar/09-uygulama-kayitlari/PAKET-S-1-uygulama-raporu.md` | **YENİ** | Planın adlandırdığı kapı dosyası — **ikinci kopya değil**, bu rapora işaret eden tek satırlık yönlendirme. |

---

## § 2 — KİMLİK KAPANIŞ TABLOSU (zorunlu)

| Kimlik | Durum | Kanıt |
|---|---|---|
| **R-3** — S-1 ticari paket sürümü (1.0, belge hattında paralel) | **KAPANDI** | Dört belge yazıldı ve kod hattına hiç dokunulmadı: bu pakette değişen tek kod-dışı olmayan dosya `dotnet/denetim.sh`'tır (sayaç bloğu). `dotnet build` 0 uyarı/0 hata, tam süit 3938/3938 yeşil → belge hattı kod hattını **bloklamadı**. |
| **R-8** — konumlandırma toplam sahip olma maliyetine dayanır; tek rakamlı "silme/tasarruf oranı" ilan edilmez | **KAPANDI** | `docs/URUN-ANLATIMI.md` § 2: Dezy 500 ₺/ay tabanına karşı **fiyat üstünlüğü YOK**, M&L **%85 iddiası ≈%54** — ikisi de "kullanılmaz" olarak yazılı. Mekanizma: `denetim.sh` `S1_RAKAM_ILANI = 0` sayacı; mutasyon **M29-4** ile kırmızıya döndüğü kanıtlandı. |
| **D-8** — ürün sayfası anlatımı "8 acı nokta → çözümü" | **KAPANDI** | `docs/URUN-ANLATIMI.md` § 1 — 8 madde, her biri kaynak taramasına (PAZAR-ARASTIRMASI-2026-09-16 § acı noktalar) ve depodaki dosya kanıtına bağlı. Kanıtı bulunamayan acı noktası listeye **alınmadı**. |

---

## § 3 — SAPMALAR VE GEREKÇELERİ (md.1 — gizlenmedi)

### S-1) `HARIC_BEYAN_KOPYA` planda **0**, bugün ölçülen **2** — çelişki yazılıyor

Plan § P-29 sayacı `HARIC_BEYAN_KOPYA = 0` diyor. **Taze ölçüm 2:**

| Dosya:satır | Metin (kısaltılmış) | Kimin eli |
|---|---|---|
| `docs/KULLANIM_KILAVUZU.md:396` | *"Banka canlı entegrasyonu (açık bankacılık) ve e-Fatura entegrasyonu **yol haritasındadır**…"* | **P-33** |
| `docs/MIMARI.md:369` | *"**Yol haritası**: e-Fatura entegrasyonu, açık bankacılık…"* | **P-33** |

İkisi de kısıtın **ikinci kaynağıdır** (BK-26'nın yasakladığı şey) ve ikisi de
plan § 7 uyarınca **P-33'ün tek elindedir**: *"`docs/` + kılavuz | P-33 tek eldir…
**P-29 ayrı dosyalara yazar**"*. P-29 bu dosyalara dokunamaz.

**Karar:** sayaç **hedef 0 / bugün 2** olarak kuruldu; tavan **yalnız azalır**,
miras dosyalar betiğin yorumunda **adıyla** yazılı, **yeni** bir elle yazılmış
kısıt beyanı kapıyı kırar (mutasyon M29-2 ile kanıtlandı). Kalan 2 kopyanın
temizliği **DEVREDİLDİ: P-33**. Tavan hiçbir yerde artırılmadı.

### S-2) Sayaç kapsamı: ürün ekranı tarafı P-30/P-32'de

`HARIC_BEYAN_KOPYA` yalnız `docs/*.md` üzerinde ölçülür. Ürün ekranı / demo
giriş ekranı tarafı **BK-47** gereği P-30/P-32'nin `PILOT_KISIT_BEYANSIZ_DAVET`
sayacındadır; aynı şey iki sayaçta sayılmadı. (İlk taramada `Pages/Hurda.razor:29`
çıkmıştı — bu bir **kod yorumudur** ("eritme firesi 1.0'a girmez"), müşteriye
giden kısıt beyanı değildir; kapsam dışı bırakıldı ve burada yazıldı.)

### S-3) D-8 için planın dosya listesinde dosya yoktu

Plan P-29'un dosya listesinde `HIZLI-BASLANGIC.md`, `SLA.md`, `HARIC-LISTESI.md`
ve rapor var; ama paketin **adı** "…+ ÜRÜN ANLATIMI" ve **D-8** kimliği bu pakete
atanmış (plan:840). Kimliği kapatmak için `docs/URUN-ANLATIMI.md` açıldı — plan
§ 7'nin "P-29 **ayrı dosyalara** yazar" iznine uygun, mevcut hiçbir belgeye
dokunmadan. Kapsam büyütülmedi: yeni ekran/uç/modül açılmadı.

### S-4) Rapor dosyası adı

Plan `PAKET-S-1-uygulama-raporu.md` diyor, görev metni `PAKET-P-29-uygulama-raporu.md`.
İkisi de üretildi; **tek gerçek metin** P-29 dosyasındadır, S-1 dosyası ona
işaret eder (ikinci kopya üretmemek için).

### S-5) "Süre iki kez ölçülür" koşulu **yarım** — gizlenmiyor

BK-29 iki ölçüm istiyor (ekip üyesi + ürünü hiç görmemiş kişi). Bugün yapılan
ölçüm **API seri yolunun makine tabanıdır** (5 sn / 8 istek), insan süresi
değildir. Ekrandan koşum ve acemi kullanıcı koşumu **yapılmadı** — ikincisi
insan gerektirir (kurucu kalemi). Bu yüzden `HIZLI-BASLANGIC.md` § 0'da tablo
**boş hücreleriyle** duruyor ve belge **hiçbir yerde süre rakamı vaat etmiyor**.
→ **DoD'un bu maddesi KISMEN.**

---

## § 4 — DoD SÖZLEŞMESİ (Bertan koşulu)

Bu paket **hiçbir yeni varlık / kayıt türü / uç / ekran üretmemiştir**; veri
modeline, servise, controller'a, Razor sayfasına tek satır eklenmedi.

| Üretilen | Tür | Düzenlenebilirlik |
|---|---|---|
| `docs/HARIC-LISTESI.md`, `docs/SLA.md`, `docs/HIZLI-BASLANGIC.md`, `docs/URUN-ANLATIMI.md` | **Depo belgesi** (veritabanı kaydı değil) | Dosya olarak düzenlenebilir; değişiklik kuralı `HARIC-LISTESI.md` § 4'te yazılı. PATCH/DELETE uç sözleşmesi **uygulanamaz** — kullanıcı arayüzünden oluşturulan bir kayıt değildir. |
| `denetim.sh` P-29 bloğu | **Kapı betiği** | Tavanlar yalnız azalır (cırcır). |

**Dokunulan sayfalar (TEK DOKUNUŞ / MÜHÜR 2):** **yok** — hiçbir `.razor`
açılmadı. Finansal varlıkta hard DELETE sorusu doğmadı.

---

## § 5 — DERLEME

```
cd dotnet && dotnet build Kuyumcu.sln --nologo
→ 0 Uyarı · 0 Hata (çıkış kodu 0)
```

---

## § 6 — TESTLER

| Kapsam | Toplam | Başarılı | Başarısız | Atlanan |
|---|---|---|---|---|
| Tam süit (`Kuyumcu.Tests`, `--logger trx`) | **3938** | **3938** | **0** | 0 |

TRX: `…/scratchpad/trx-pP-29/_Ramazan-MacBook-Air_2026-09-23_12_53_01.trx`
→ `Counters total="3938" passed="3938" failed="0" error="0"`.

**Yeni C# testi: 0.** Sebebi dürüstçe: bu paket kod üretmedi; iş kuralları
(tek kaynak · damga · rakam yasağı · göç kararının yazılı olması) **metin
kurallarıdır** ve `denetim.sh` sayaçlarıyla sınanır. Her kuralın "hatayı yeniden
üreten testi" § 7'deki mutasyon tablosudur — dördü de kırmızıya döndü.

**Önce/sonra:** süit sayısı değişmedi (3938 → 3938); belge paketi test tabanını
büyütmez.

---

## § 7 — MUTASYON KANITLARI (4/4, hepsi geri alındı ve `cmp` ile doğrulandı)

| Kod | Değişiklik | Kırılan kapı (gerçek çıktı) | Geri alma |
|---|---|---|---|
| **M29-1** | `HARIC-LISTESI.md`'de `## 2. GÖÇ KARARI` başlığı silindi | `✗ P-29: HARIC_KAYNAK_BOZUK (beklenen 0) — H satırı 8/8 · göç kararı 0/1 — BK-31 sessiz bırakılamaz` → `DENETİM BAŞARISIZ` | `cmp` **birebir** |
| **M29-2** | `SLA.md`'deki `HARIC-KAYNAK` işareti kaldırılıp yerine elle kısıt beyanı yazıldı | `✗ P-29: HARIC_BEYAN_KOPYA (tavan 2) — 3 — KULLANIM_KILAVUZU.md MIMARI.md SLA.md · ikinci elle yazılmış kısıt beyanı (BK-26)` | `cmp` **birebir** |
| **M29-3** | `URUN-ANLATIMI.md`'den "TASLAK — ilan edilmemiştir" damgası kaldırıldı | `✗ P-29: S1_BELGE_DAMGASIZ (beklenen 0) — 1` | `cmp` **birebir** |
| **M29-4** | `URUN-ANLATIMI.md`'ye *"Mikro Jump kullananlar bizde %86 tasarruf sağlar"* satırı eklendi | `✗ P-29: S1_RAKAM_ILANI (beklenen 0) — 1 — S-1 metninde rakamlı iddia (R-8 · FAIL-CLOSED 1 · K-16)` | `cmp` **birebir** |

**TEST AÇIĞI BULUNDU VE KAPATILDI (md.1):** M29-2'nin **ilk** denemesi
kırmızıya dönmedi. Sebep: dedektör yalnız noktalı **"HARİÇ"**i arıyordu, mutasyon
metni **"HARIÇ"** (noktasız I) yazılmıştı. Sayaç deseni `HARİÇ|HARIÇ|HARIC` ve
`kapsam dışı|kapsam disi` olacak biçimde **güçlendirildi**, taban yeniden ölçüldü
(2, değişmedi) ve mutasyon tekrarlandı → kırmızı.

Son mutasyondan sonra: `dotnet build` **0 uyarı 0 hata**, `denetim.sh` **TEMİZ**.

---

## § 8 — DENETİM (dotnet/denetim.sh)

Çıkış kodu **0**, son satır **`DENETİM TEMİZ.`** P-29 satırları:

```
  ✓ P-29: HARIC_KAYNAK_BOZUK       0 (8 kısıt satırı + göç kararı yerinde)
  ✓ P-29: HARIC_BEYAN_KOPYA        2/2 miras — KULLANIM_KILAVUZU.md MIMARI.md (hedef 0 · devir P-33)
  ✓ P-29: S1_BELGE_DAMGASIZ        0 (4 belge · TASLAK damgası yerinde)
  ✓ P-29: S1_RAKAM_ILANI           0 (tek rakamlı tasarruf/fiyat iddiası yok — R-8)
```

Her sayacın **ölçüm komutu** betiğe yorum olarak yazıldı ve tabanlar **bugün
taze ölçüldü** (belgeden kopyalanmadı). Mevcut hiçbir tavan artırılmadı.

---

## § 9 — CANLI DOĞRULAMA (localhost:5198 — **5199'a DOKUNULMADI**)

Kiracı: **PAKETP29-TEST Kuyumculuk** (`01M36TCNTY3ATPFNT30KS12X18`), davet → kayıt
yoluyla açıldı, iş sonunda silindi. Koşum: **12:43:45 → 12:43:50 (5 sn)**.

| # | Adım | Beklenen | Gerçek | Kanıt |
|---|---|---|---|---|
| A1 | `POST /api/v1/sistem/davetler` | 201 + tek kullanımlık jeton | **201**, `durum=BEKLIYOR`, `jetonUyarisi` dolu | sistem anahtarı + operatör başlığı ile |
| A2 | `POST /api/v1/auth/kayit` (davetli) | 201, kiracı açılır | **201**, `tenantId=01M36TCNTY3ATPFNT30KS12X18` | — |
| A3 | `GET /api/v1/kur/guncel` | kur ~10-20 sn içinde gelir | **~5 sn** sonra `id=01M36TCS3VRZX6AG85NVN4PGCB` | tuzak listesine yazıldı |
| A4 | `GET /api/v1/kasalar` | — | **`[]`** — yeni kiracıda kasa YOK | `HIZLI-BASLANGIC.md` § 2/1 |
| A4b | `POST /api/v1/kasalar` | kasa açılır | **200**, `Ana Kasa`, `bakiye=0.00` | şube "Merkez Şube" kayıtla otomatik gelmiş |
| A5 | `POST /api/v1/urunler` | 201 | **201** (22 ayar, 10,5 g) | — |
| A6 | `POST /api/v1/islemler/alis` (40.000 TL nakit) | tevsik haddi durdurur | **400** — *"toplam tutarı 40.000,00 TL, 30.000,00 TL haddini aşıyor…"* | yasal kapı çalışıyor |
| A6b | aynı uç (20.000 TL) | 200, stok girer | **200**, `hasGrami=9,618` | — |
| A7 | `POST /api/v1/cari` | 201 | **201** | — |
| A8 | `POST /api/v1/islemler/satis` (stok yokken) | 400 | **400** — *"Bu şubede ürün stoğu bulunamadı"* | — |
| A9 | **İLK SATIŞ** `POST /api/v1/islemler/satis` | 200 | **200**, `tlTutari=26.000,00`, `hasGrami=−9,618`, `odemeTuru=NAKIT` | — |
| A10 | Kayıt ucu 4. kez | oran sınırı | **429** — *"Çok fazla istek gönderildi"* | `OranSiniri.cs:120-122` (3/saat) |

**SQL kanıtı (aynı kiracı):**
`hareketler=2 · kasa_hareketleri=2 · urunler=1 · cariler=1 · denetim_kayitlari=23`

**Temizlik:** API PID öldürüldü → `lsof -nP -iTCP:5198 -sTCP:LISTEN` **boş**;
`lsof … :5199` hâlâ dolu (**kurucunun sunucusu — dokunulmadı**).
`tenant_id` kolonu olan **70 tablo** + `kiraci_davetleri` + `tenants` temizlendi.
**`tenants` 9 → 9** (başlangıçtaki değer). **`QA-TEST-*` fikstürleri: 4/4 duruyor.**

---

## § 10 — DEĞİŞEN DOSYALAR

`git status --porcelain | grep -v -E "/(bin|obj)/"` (bu paketin dokunduğu satırlar):

```
?? docs/HARIC-LISTESI.md
?? docs/HIZLI-BASLANGIC.md
?? docs/SLA.md
?? docs/URUN-ANLATIMI.md
?? dotnet/denetim.sh                     (izlenmeyen dosya; P-29 bloğu eklendi)
?? dokumanlar/09-uygulama-kayitlari/     (P-29 + S-1 raporları)
```

**COMMIT YOK.** Çalışma ağacında kalındı.

---

## § 11 — KOŞUL KARŞILAMA TABLOSU

| Koşul | Durum | Kanıt |
|---|---|---|
| SLA yalnız bugün taahhüt edilebileni yazar; kanıtsız sayı yok | **KARŞILANDI** | `SLA.md` üç kutu; RTO/RPO/uptime/destek/kapasite **taahhüt yok** kutusunda |
| Yedek sıklığı / geri yükleme hedefi P-33'e dayanır | **KARŞILANDI** | `SLA.md` Y-1…Y-4 "yapılandırılmış, ölçülmemiş"; timer dosyası satırıyla kanıtlı |
| Hızlı başlangıç süresi **ölçülür**, iki kez | **KISMEN** | 1 makine-tabanı koşumu yapıldı; ekran ve acemi koşumu **açık** (§ 3 S-5) |
| R-8: tek rakamlı oran hiçbir kanalda ilan edilmez | **KARŞILANDI** | `URUN-ANLATIMI.md` § 2 + `S1_RAKAM_ILANI=0` sayacı + M29-4 |
| HARİÇ kalemler tek kaynakta (e-Fatura, ÖKC, SMS, banka API, imalat) | **KARŞILANDI** | `HARIC-LISTESI.md` H-1…H-8, her biri dosya kanıtlı |
| Göç kararı yazılı (BK-31) | **KARŞILANDI** | `HARIC-LISTESI.md` § 2: CSV cari+ürün **VAR**, açılış bakiyesi (cari/kasa/banka) **VAR**, **açılış stoku HARİÇ** |
| "TASLAK — ilan edilmemiştir" damgası | **KARŞILANDI** | 4/4 belge; `S1_BELGE_DAMGASIZ=0` + M29-3 |
| `HARIC_BEYAN_KOPYA = 0` | **KISMEN** | bugün **2** miras (P-33'ün dosyaları); tavan cırcır, yeni kopya kapıyı kırar (§ 3 S-1) |
| Kapsam kilidi — başka paketin kimliğine dokunulmadı | **KARŞILANDI** | Kod/ekran/uç değişikliği **yok**; yalnız 4 yeni belge + denetim bloğu |
| Migration HAYIR | **KARŞILANDI** | Şema değişikliği yok; `__EFMigrationsHistory` sorgulanmadı, migration üretilmedi |
| Tam süit 0 başarısız | **KARŞILANDI** | 3938/3938 |
| `denetim.sh` TEMİZ | **KARŞILANDI** | çıkış 0 |
| Test kiracısı silindi, `QA-TEST-*` korundu | **KARŞILANDI** | tenants 9 → 9; QA fikstürü 4/4 |
| 5199'a dokunulmadı | **KARŞILANDI** | yalnız 5198 kullanıldı; 5199 dinleyicisi koşumdan sonra da yerinde |

---

## § 12 — AÇIK KALANLAR (devredilenler)

1. **`HARIC_BEYAN_KOPYA` 2 → 0** — `docs/KULLANIM_KILAVUZU.md:396` ve
   `docs/MIMARI.md:369` cümlelerinin `HARIC-LISTESI.md`ye yönlendirmeye
   çevrilmesi. **DEVREDİLDİ: P-33** (o dosyaların tek eli).
2. **Hızlı başlangıç Ö-2 ölçümü** (ekip üyesi, tarayıcıdan) — **DEVREDİLDİ:
   P-30/P-32 hattı** (demo provası ile aynı koşumda ölçülebilir).
3. **Hızlı başlangıç Ö-3 ölçümü** (ürünü hiç görmemiş kişi) — **KURUCU
   KALEMİ**: insan gerektirir; pilot davetiyle (K-19) birlikte yapılmalı.
4. **SLA kutularının taşınması** — Y-2/Y-3/Y-4 P-33'ten, N-2 P-32'den,
   N-3 P-25'ten kanıt bekliyor.
5. `Pages/Hurda.razor:29` kod yorumundaki "imalat/atölye kapsam dışı" ifadesi —
   kısıt beyanı sayılmadı; ürün ekranı tarafının denetimi **P-30/P-32**
   (`PILOT_KISIT_BEYANSIZ_DAVET`, BK-47).

---

## § 13 — KURUCUYA / MALİ MÜŞAVİRE NOTLAR

- **K-16 (rakamlı fiyat ilanı):** bu paketin ürettiği dört belgenin hiçbirinde
  fiyat yok ve `denetim.sh` bunu sayaçla kilitliyor. Ürün ticari olarak
  "TASLAK" rafındadır; kilidi yalnız sizin imzanız açar — **ve** F-8'in
  2 ölçülmemiş satırı kapanmadan açılmamalıdır (FAIL-CLOSED 1).
- **K-2 / K-11 / K-15 / K-22:** e-Fatura, ÖKC, SMS, banka API kalemleri
  `HARIC-LISTESI.md`de "hariç" olarak **yazılı ve kanıtlı** duruyor. Sözleşme
  gelmeden ekip bunların yerine geçici çözüm uydurmadı.
- **Mali müşavire:** `HARIC-LISTESI.md` H-8 (KMTS/bandrol) ve `URUN-ANLATIMI.md`
  § 1.7 (gerçek usule geçiş) mevzuat özetleri **birincil kaynak okumasına**
  dayanıyor ama **hukuki görüş değildir**; ürün hiçbir yerde "KMTS uyumluyuz"
  iddiası etmiyor. Perakende kuyumcunun KMTS kapsamına girip girmediğinin
  yazılı teyidi hâlâ açık (**K-14**).
- **Köprü'ye:** pilot davet metni ve demo giriş ekranı kısıt beyanını
  **`HARIC-LISTESI.md`den gömerek** üretmelidir; elle ikinci bir cümle yazılırsa
  kapı kırılır.

---

## § 14 — BÜTÇE

| Ölçü | Tahmin | Gerçekleşen (yaklaşık) | Not |
|---|---|---|---|
| Ajan-dakika | 240 | **~70** | Belge hattı; kod yazılmadı, derleme/test yalnız doğrulama için koştu. Canlı ölçüm koşumunda 3 yeniden deneme oldu (BSD `head -n -1`, zsh `UID` çakışması, kayıt oran sınırı) — hepsi rapora yazıldı.

---

## § 15 — DOĞRULAMA TURU 2 DÜZELTMELERİ (2026-09-23, hasım doğrulayıcı bulguları)

Doğrulayıcı dört bulgu bıraktı. Hepsi aşağıda; **hiçbiri gizlenmedi.**

### B-1 [KRİTİK] — SLA.md'de ÖLÇÜLMEDEN yazılmış iki rakam · **KAPANDI**

Bulgu doğrulandı: `docs/SLA.md` § 1 "TAAHHÜT EDİLEBİLİR (kanıtlı)" tablosunun
kanıt sütunundaki iki rakam görev metnindeki eski değişmez cümlesinden
kopyalanmış, hiç ölçülmemişti.

| Satır | Eski (yanlış) | Taze ölçüm | Yeni metin |
|---|---|---|---|
| `docs/SLA.md:28` (S-1) | "`IgnoreQueryFilters` **beyaz listede 4** ve artamaz" | `grep -rh "\.IgnoreQueryFilters()" --include='*.cs' Kuyumcu.Api \| grep -c .` → **0** | "`IgnoreQueryFilters` kullanımı **0** (denetim.sh:44 ölçümü; cırcır artmaya kapalı)" |
| `docs/SLA.md:30` (S-3) | "denetim.sh `ExecuteDelete` beyaz listesi **4**" | `denetim.sh:65` → `denetle "ExecuteUpdate/Delete (beyaz liste 5)" … 5` → **5** | "denetim.sh `ExecuteUpdate/Delete` beyaz listesi **5** (denetim.sh:65; cırcır artmaya kapalı)" |

Kök neden doğrulayıcının teşhisiyle aynı: P-29'un dört sayacı da rakamların
**değerine** bakmıyordu; yalnız varlık/damga/iddia denetliyordu. Bu yüzden
belge yanlışken denetim TEMİZ çıkıyordu.

### B-1b — Beşinci cırcır sayacı: `SLA_OLCULMEMIS_RAKAM = 0` · **EKLENDİ**

`dotnet/denetim.sh` P-29 bloğuna eklendi (ölçüm komutları betiğe yorum olarak
yazıldı, taban TAZE ölçüldü — belgeden kopyalanmadı). Kural: `docs/SLA.md`
§ 1 tablosundaki **her kalın rakam** ya betikteki bir ölçüm komutuyla yeniden
ölçülür ve eşleşir, ya da canlı-koşum etiketi + tarihi taşır
(`Canlı koşum (20…)` — S-4'ün 23 satırı budur). Kayıtsız satırda kalın rakam
görürse **kırmızı** olur; yani P-32/P-33 SLA'ya yeni bir ölçülmemiş sayı
yazarsa kapı kapanır.

Taze çıktı:

```
  ✓ P-29: HARIC_KAYNAK_BOZUK                       0 (8 kısıt satırı + göç kararı yerinde)
  ✓ P-29: HARIC_BEYAN_KOPYA                        2/2 miras — KULLANIM_KILAVUZU.md MIMARI.md (hedef 0 · devir P-33)
  ✓ P-29: S1_BELGE_DAMGASIZ                        0 (4 belge · TASLAK damgası yerinde)
  ✓ P-29: S1_RAKAM_ILANI                           0 (tek rakamlı tasarruf/fiyat iddiası yok — R-8)
  ✓ P-29: SLA_OLCULMEMIS_RAKAM                     0 (§1 kalın rakamların hepsi taze ölçümle eşleşti)

DENETİM TEMİZ.
```

**Yeni mutasyon kanıtları (§ 7'ye ek — toplam 6/6):**

| Kod | Değişiklik | Kırmızıya dönen | Geri alma |
|---|---|---|---|
| M29-5 | `docs/SLA.md:28` → "**0**" yerine "**4**" (bulgunun kendisi yeniden üretildi) | `✗ P-29: SLA_OLCULMEMIS_RAKAM (beklenen 0) 1 — S-1:yazılan=4/ölçülen=0` → `DENETİM BAŞARISIZ` | geri alındı; `diff` ile SLA.md yalnız 28/30'da yedekten ayrılıyor (kasıtlı düzeltme) |
| M29-6 | `docs/SLA.md:30` → "**5**" yerine "**4**" | `✗ P-29: SLA_OLCULMEMIS_RAKAM (beklenen 0) 1 — S-3:yazılan=4/ölçülen=5` → `DENETİM BAŞARISIZ` | geri alındı; denetim yeniden TEMİZ |

### B-2 [ORTA] — `HARIC_BEYAN_KOPYA = 2` · **DEVREDİLDİ: P-33 (tavan 2, artırılmadı)**

Doğrulayıcı sapmayı kabul etti (plan § 7: `docs/KULLANIM_KILAVUZU.md` ve
`docs/MIMARI.md` **P-33'ün tek elidir**, P-29 dokunamaz). Talep edilen kayıt
burada resmileştirildi — **kapı kaydına ve P-33 briefine bağlayıcı çıkış
koşulu olarak girer:**

> **DEVİR-P29-1 (bağlayıcı):** P-33, `docs/KULLANIM_KILAVUZU.md:396` ve
> `docs/MIMARI.md:369` cümlelerini `docs/HARIC-LISTESI.md`ye yönlendirmeye
> çevirir; `HARIC_BEYAN_KOPYA` tavanı **2 → 0** indirilir. Tavan
> **artırılamaz**. P-33 bu koşulu kapatmadan sürüm mührü atılmaz.
>
> **DEVİR-P29-2 (bağlayıcı):** `docs/MIMARI.md:369` ürünü "15 tablo, 14 API
> modülü, 10 web sayfası" diye anlatıyor; bugünkü ürün **66 migration**lık.
> Metin gerçeğe göre düzeltilir — P-33.

### B-3 [ORTA] — BK-29 "iki kez ölçüm" · **YARIM (gizlenmiyor), devredildi**

Doğrulama sonucu teyit edildi: yalnız **Ö-1 makine tabanı** koşuldu (5 sn / 8
istek, insan süresi değil). `docs/HIZLI-BASLANGIC.md` § 0 tablosu **boş
bırakılmış** durumda (Ö-2 ve Ö-3 satırları açıkça **YAPILMADI**) ve belge
hiçbir yerde süre vaat etmiyor — `S1_RAKAM_ILANI 0` ve M29-4 bunu doğruluyor.
Kayıt:

> **DEVİR-P29-3:** Ö-2 (ekip üyesi, ekrandan) ölçümü **P-30/P-32 demo provası
> hattının bağlayıcı çıkış koşuludur.**
> **DEVİR-P29-4:** Ö-3 (ürünü hiç görmemiş kişi) **kurucu kalemidir**; pilot
> davetiyle (K-19) birlikte yapılır. İkisi tamamlanana dek § 0 tablosu boş
> kalır ve **hiçbir kanalda süre telaffuz edilmez.**

### B-4 [DÜŞÜK] — miras mutasyon artıkları ve 0 baytlık migration dosyaları · **KURUCU KALEMİ**

Doğrulandı ve **hiçbirine dokunulmadı** (hook `rm`/`mv` bloklar; ayrıca P-29
kapsamı dışı — hepsi 2026-09-21 damgalı, P-29 yalnız `docs/` + `denetim.sh`
yazdı). Kayıt: 16 `*.mutorj` + 3 `*.bak` + `Data/Migrations` altında 8 adet
0 baytlık dosya (`…_FiyatListesiVeKampanya.cs`/`.Designer.cs` × 4).
**DEVİR-P29-5:** orkestratör kapısına "miras artık temizliği" kalemi; 0 baytlık
4 çift migration'ın `__EFMigrationsHistory`'de karşılığı olup olmadığı ayrıca
doğrulanmalı. Silme yetkisi **kurucudadır.**

### Turu 2 sonrası doğrulama koşumları (taze)

| Kontrol | Sonuç |
|---|---|
| `dotnet build Kuyumcu.sln --nologo` | **0 Hata · 0 Uyarı** |
| `dotnet test Kuyumcu.Tests` (tam süit) | `Başarılı! - Başarısız: 0, Başarılı: 3938, Atlanan: 0, Toplam: 3938` |
| `dotnet/denetim.sh` | **DENETİM TEMİZ** (çıkış 0), P-29 sayaç sayısı 4 → **5** |
| Canlı (5198) | **Bu turda açılmadı.** Gerekçe (md.1 — varsayım değil): bu tur yalnız `docs/SLA.md` metni + `denetim.sh` sayacı değiştirdi; çalışan yüzey (API/Web/DB) değişmedi, `git status` bunu doğruluyor. Düzeltilen iki rakam **kod ölçümüdür** ve yukarıdaki grep/sed komutlarıyla doğrudan doğrulandı. § 9'daki 8 adımlık 5198 koşumu geçerliliğini korur; yeni test kiracısı açılmadı, silinecek kiracı yok. |

**Bu turda değişen dosyalar:** `docs/SLA.md` (2 satır) ·
`dotnet/denetim.sh` (P-29 bloğuna 5. sayaç) ·
`dokumanlar/09-uygulama-kayitlari/PAKET-P-29-uygulama-raporu.md` (bu bölüm).
