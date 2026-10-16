# PAKET P-D1 — ÖLÜ İÇ BAĞLANTI + PROVA ROTA ETİKETİ (uygulama raporu)

- **Tarih:** 2026-09-23
- **Dalga:** DEMO HAZIRLIK (cumartesi müşteri demosu — zorunlu işler)
- **Kaynak:** `dokumanlar/02-mimari/sadelestirme/DEMO-SENARYOSU-2026-09-26.md` § 4 → **D-b** ve **D-c** (ayrıca § 0 / Z2)
- **Rol:** Taşkın (backend) + Ertuğ (frontend)
- **Durum:** kod işi **BİTTİ ve kanıtlandı**; tam süit ve denetim kapısı **BAŞKA ŞERİTLERİN yarım işleri yüzünden kırmızı** (aşağıda birebir kanıt; hiçbiri bu paketin dosyalarından gelmiyor).

---

## 1. Yapılanlar

### D-b — Panel'deki tek ölü iç bağlantı
`dotnet/Kuyumcu.Web/Pages/Panel.razor:273`

```
- <a class="metin-kucuk" href="/islem-defteri"          id="panel-defter-git">…</a>
+ <a class="metin-kucuk" href="/raporlar/islem-defteri" id="panel-defter-git">…</a>
```

Gerçek rota `Pages/IslemDefteri.razor:1` → `@page "/raporlar/islem-defteri"`.
Ölçüm: uygulamadaki **tek** ölü düz metin iç bağlantı buydu (bkz. § 5 sayaç çıktısı) ve demonun **2. ekranındaydı**.

### D-c — `demo-tohumla.js` prova tablosundaki yanlış rota etiketi
`dokumanlar/09-uygulama-kayitlari/betikler/demo-tohumla.js` — adım 8:

```
- { no: 8, rota: '/vade-takibi',           yol: '/raporlar/vade-takibi', … }
+ { no: 8, rota: '/raporlar/vade-takibi',  yol: '/raporlar/vade-takibi', … }
```

**"İkisini tek kaynaktan üret" nasıl yorumlandı (sapma değil, tanım):** `yol` bir **API ucudur**, `rota` bir **ekran adresidir**; ikisi aynı şey değildir (ör. adım 9: `rota: '/raporlar'`, `yol: '/raporlar/gunluk'`). Bu yüzden "rota = yol" yapılamazdı. Tek kaynak şöyle kuruldu: **`rota` artık uygulamanın `@page` direktiflerine karşı doğrulanır.** Betiğe eklenenler:

- `uygulamaRotalari()` — `dotnet/Kuyumcu.Web/Pages/*.razor` içindeki `@page` satırlarını okur, `{Id:int}` → `{Id}` sadeleştirir.
- `provaRotalariniDogrula()` — her adımın `rota` etiketi bu kümede yoksa **prova BAŞLAMAZ** (fail-closed); ayrıca ölçüm 40'ın altına düşerse "sayaç oyunlanmış" diye reddeder.
- `prova()` içinde **çağrılır** ve sonuç rapor JSON'una yazılır: `rota_tek_kaynak: { agac_rota_sayisi: 46, olu_rota_etiketi: 0 }`.

Canlı sınama (node, ağaç üzerinde):

```
GERCEK rota sayisi: 46
KAPI KAPANDI -> REDDEDİLDİ: prova tablosunda ÖLÜ ROTA etiketi var: /vade-takibi.
                Bu adresler uygulamanın @page listesinde YOK
```

### KAPI — `OLU_IC_BAGLANTI = 0` denetim sayacı
`dotnet/denetim.sh`, P-02 bloğuna iki sayaç eklendi:

| Sayaç | Ne ölçer |
|---|---|
| **P02-1b `OLU_IC_BAGLANTI`** | `href="/…"` ve `NavigateTo("/…")` hedeflerinin tamamı `@page` kümesinde mi (`Pages · Bilesenler · Layout · Servisler`) |
| **P02-1c** | `demo-tohumla.js`'teki tek kaynak kapısının **sökülmediği** (sayacın sayacı) |

Neden yeni bir sayaç gerekti: **P02-1** rota *sayısını*, **P-09** rota↔yetki *eşleşmesini* ölçüyordu; "bu bağlantı bir yere gidiyor mu" sorusu **hiç ölçülmüyordu**. Kapı olmadan aynı hata yarın geri gelirdi.

**Sayacın açıkça yazılı sınırları (Anayasa md.1 — kapsam gizlenmez):**
1. Yalnız **düz metin** hedefler. `href="@(...)"` gibi enterpolasyonlu hedefler statik çözülemez, sayılmaz.
2. Sorgu/çapa soyulur (`/gider?sekme=virman` → `/gider`).
3. **Uzantılı hedef statik varlıktır, rota değildir** (`/kilavuz.html`, `/img/ikon.svg#…`); bunların sunucuda var olup olmadığı ayrı bir konudur — `/kilavuz` 404'ü S-4'ün açık kalemidir, bu kapının iddiası değildir.
4. Parametreli rotalar `{Ad}` biçimine indirgenir ve parça sayısı + `{…}` joker ile eşlenir.

---

## 2. Spesifikasyonun koda karşı doğrulanması

| Spesifikasyon iddiası | Kodda bugün | Sonuç |
|---|---|---|
| `Panel.razor:273` → `href="/islem-defteri"` | **Satır 273, birebir doğru** | Doğrulandı |
| Gerçek rota `@page "/raporlar/islem-defteri"` | `Pages/IslemDefteri.razor:1` | Doğrulandı |
| "Uygulamadaki **tek** ölü iç bağlantı" | Tarama: 22 düz metin hedef, 46 rota → **yalnız 1 ölü** | Doğrulandı |
| `demo-tohumla.js:131` `rota: '/vade-takibi'` | Satır **131** (kayma yok) | Doğrulandı |
| Menü rotaları (`MenuTanimi.cs`, `BasitMenu.cs`) | Tarandı → **0 ölü** | Ek doğrulama, sapma yok |

**Mevzuat/hüküm sabitleme:** bu paket hiçbir yasal/mevzuat hükmü içermiyor (salt bağlantı/etiket düzeltmesi + denetim kapısı). Yapılandırmaya taşınacak bir hüküm yok.

**Migration:** **gerekmedi** — veri modeli değişmedi, yeni varlık yok.

**DoD sözleşmesi (yeni varlık/kayıt türü):** bu pakette **yeni varlık/kayıt türü doğmadı**, dolayısıyla PATCH/DELETE/yetki/denetim kaydı yükümlülüğü **doğmadı**. Değişen tek kullanıcı arayüzü ögesi mevcut bir bağlantının `href` değeridir; yeni düğme, yeni form, yeni alan eklenmedi — UX-S serisinde bu paketten doğan bir yükümlülük yoktur. Tek dokunuş kaydı aşağıda.

**Dokunulan sayfalar (DoD EK 2 — TEK DOKUNUŞ):** `Pages/Panel.razor` (yalnız 273. satırdaki `href` değeri; tasarım/düzen/başka satır değişmedi).

---

## 3. Derleme

```
dotnet build Kuyumcu.sln --nologo
Oluşturma başarılı oldu.
    0 Uyarı
    0 Hata
```
(16:2x koşumu — `$S/b3.txt` koşumunda 2 uyarı görüldü: **`Kuyumcu.Tests/OlcumKapisiTestleri.cs(525,32)` ve `(525,52)` CS8604**. Bu dosyaya bu paket DOKUNMADI; uyarı başka bir şeridin yarım düzenlemesinden gelip gitti, temiz koşumda 0 uyarı ölçüldü.)

---

## 4. Testler

**Yeni test dosyası:** `dotnet/Kuyumcu.Tests/OluIcBaglantiTestleri.cs` — **6 yeni test**.

| Test | Ne kilitler |
|---|---|
| `Hicbir_ic_baglanti_OLU_degildir` | ASIL KAPI: her düz metin iç bağlantı `@page` kümesinde |
| `Baglanti_taramasi_BOS_donmez` | Ölçüm ≥ 15 sonuç döndürmeli (fail-open koruması) |
| `Panel_son_islemler_baglantisi_islem_defteri_rotasina_gider` | D-b geri dönüş testi — **doğru** adres |
| `Denetim_betiginde_OLU_IC_BAGLANTI_sayaci_durur` | denetim.sh sayacı sökülemez |
| `Demo_prova_tablosunda_OLU_rota_etiketi_yoktur` | D-c: prova etiketleri `@page` kümesinde |
| `Demo_betigi_prova_rotalarini_page_listesine_karsi_dogrular` | Kapı **tanımlı + ÇAĞRILIYOR** (≥2 geçiş) |

Filtreli koşum (`KUYUMCU_TEST_DB=kuyumcu_test_d`):

```
Başarılı!  - Başarısız: 0, Başarılı: 6, Atlanan: 0, Toplam: 6, Süre: 116 ms
```

### Tam süit — DÜRÜST KAYIT (Anayasa md.1)

```
<Counters total="4006" executed="4006" passed="2276" failed="1730" … />
```

**Bu 1730 kırmızının hiçbiri bu pakete ait değildir.** Kanıt:

1. **Baskın kök neden (3216 yığın izinde):**
   `MySqlConnector.MySqlException : Unknown column 'sema' in 'field list'`
   Ölçüm: `Kuyumcu.Api/Data/Entities.cs:308` → `[Column("sema"), MaxLength(24)] public string? Sema`
   `grep -rl '"sema"' Kuyumcu.Api/Data/Migrations/` → **BOŞ**.
   Yani **başka bir şerit varlığa kolon ekledi, migration yazmadı**; `Migrate()` ile kurulan test şemasında `sema` yok, model bekliyor → bütün DB tabanlı testler düşüyor. Bu paket veri modeline dokunmadı; migration yazmak **bu paketin kapsamı dışıdır ve o şeridin dosyalarıdır**.
2. Diğer kırmızı öbekler yine başka şeritlerin yarım işleri:
   `S3aSiparisDuzenlemeEkranTestleri` (ŞERİT A — `SiparisDetay.razor`, koşum sırasında `CS0103: 'UrunSecenekleri' … yok` ile derlemeyi de kırdı),
   `TasarimBelirtecTestleri` (tema şeridi — "tema.js 'kurumsal' şemasını yazıyor ama belirtecler.css'te blok YOK").
3. **`OluIcBaglantiTestleri` tam süit içinde 6/6 YEŞİL**, 0 FAIL.

Ayrıca koşum sırasında iki kez **MSB3030** (`Kuyumcu.Web/bin/Debug/net8.0/Kuyumcu.Web.dll … bulunamadığı için kopyalanamadı`) ve bir kez `VSTestTask … MSB4181` alındı — eşzamanlı şeritlerin aynı `bin/obj` ağacını yeniden kurmasından doğan **yarış**; kod hatası değil, yineleyerek aşıldı.

**Önce/sonra test sayısı:** 4000 → **4006** (+6).

---

## 5. Mutasyon kanıtları

Yöntem: kaynak `scratchpad/mut-yedek/P-D1/` altına kopyalandı → `perl -0777 -pi -e` ile tek eşleşmeli değişiklik → filtreli `dotnet test -v n` → yedek geri kopyalandı → `cmp` ile birebir aynılık kanıtlandı.

| Kod | Değişiklik | Kırmızıya dönen test(ler) | Geri alındı (cmp) |
|---|---|---|---|
| **M-D1-1** | `Panel.razor`: `href="/raporlar/islem-defteri"` → `href="/islem-defteri"` (arızanın kendisi) | `Hicbir_ic_baglanti_OLU_degildir` · `Panel_son_islemler_baglantisi_islem_defteri_rotasina_gider` | cmp = aynı |
| **M-D1-2** | `demo-tohumla.js`: `rota: '/raporlar/vade-takibi'` → `'/vade-takibi'` (arızanın kendisi) | `Demo_prova_tablosunda_OLU_rota_etiketi_yoktur` | cmp = aynı |
| **M-D1-3** | `demo-tohumla.js`: kapı **çağrısı** söküldü (`provaRotalariniDogrula()` → sabit 46) — fail-open | `Demo_betigi_prova_rotalarini_page_listesine_karsi_dogrular` (ayrıca denetim.sh P02-1c ✗ verdi) | cmp = aynı |
| **M-D1-4** | `denetim.sh`: `P02_OLU_N` → `P02_ATLANDI_N` (sayaç söküldü) | `Denetim_betiginde_OLU_IC_BAGLANTI_sayaci_durur` | cmp = aynı |
| **M-D1-5** | `Layout/UstBar.razor`: `href="/bildirimler"` → `href="/bildirim"` (**Pages dışı** dosya) | `Hicbir_ic_baglanti_OLU_degildir` — hata metni: `/bildirim ← UstBar.razor` | cmp = aynı |

**Test açığı bulunmadı** — her mutasyon en az bir testi kırdı. M-D1-5 ayrıca kapının `Pages/` ile sınırlı olmadığını, `Layout`/`Bilesenler`/`Servisler`'i de gerçekten taradığını kanıtlar.

Son mutasyondan sonra: `dotnet build Kuyumcu.sln --nologo` → **0 Hata**.

---

## 6. Denetim (`dotnet/denetim.sh`)

Bu paket bittiğinde (ilk temiz koşum, `denetim1.txt`) — **rc = 0**:

```
  ✓ P-02: OLU_IC_BAGLANTI (href/NavigateTo ⊆ @page)  0 (22 bağlantı / 46 rota)
  ✓ P-02: demo provası rota tek kaynak kapısı (D-c)  fail-closed
  …
DENETİM TEMİZ.
```

Son koşum (`denetim2.txt`) — **rc = 1, 1 ihlal**:

```
  ✓ P-02: OLU_IC_BAGLANTI (href/NavigateTo ⊆ @page)  0 (22 bağlantı / 46 rota)
  ✓ P-02: demo provası rota tek kaynak kapısı (D-c)  fail-closed
  ✗ P-11: DIS_HATADA_VERI_DUSUREN_YOL_TOPLAM         48 > tavan 47
```

**Bu ihlal bu pakete ait değildir, kanıt:**
- Aynı sayaç bu paketin bittiği koşumda **`✓ 47 (tavan 47)`** idi; 47 → 48 artışı iki koşum ARASINDA, başka bir şeridin yazmasıyla oldu.
- Dosya bazında ölçüm: **`Pages/Panel.razor` katkısı = 0**. Sayacı taşıyanlar `Urunler.razor` 5 · `Faturalar.razor` 5 · `Cariler.razor` 5 · … (`Faturalar.razor` ŞERİT A'nın dosyasıdır).
- Bu paketin dört dosyasının hiçbirinde `catch` içinde veri alanı boşaltan yol yoktur.

---

## 7. Canlı doğrulama

Port **5195** (izolasyon koşulu). API `--no-build` ile kaldırıldı, iş bitince yalnız kendi PID'i durduruldu.

| Adım | Beklenen | Gerçek | Kanıt |
|---|---|---|---|
| API ayakta | HTTP 200 | **200** | `GET http://localhost:5195/genel/kisitlar` → 200 (SPA geri düşümü, `index.html`) |
| `/raporlar/islem-defteri` | 200 | **200** | SPA geri düşümü — HTTP kodu iki rotayı **ayırt etmez** (aşağıya bak) |
| `/islem-defteri` | 200 | **200** | aynı sebep |
| Anonim tarayıcı denemesi | rota ayrımı | **ayırt edilemedi** | Kimlik kapısı her iki adresi de `/giris`'e çeviriyor (`Title: Giriş Yap`) |
| **Sunulan yapıtta rota dizgesi** | ölü href YOK | **KANITLANDI** | aşağıdaki bayt ölçümü |

HTTP kodu ve anonim gezinme bu arızayı ayırt edemediği için doğrulama **sunulan derlenmiş yapıt** üzerinden yapıldı (`GET /_framework/Kuyumcu.Web.wasm`, 2.551.065 bayt):

```
UTF-8    '/raporlar/islem-defteri'   → 1   (@page rota tablosu)
UTF-16LE '/raporlar/islem-defteri'   → 1   (Razor markup / href değeri)
UTF-16LE 'panel-defter-git'          → 1   (aynı markup bloğu)
UTF-8    'href="/islem-defteri"'     → 0
UTF-16LE 'href="/islem-defteri"'     → 0
UTF-8    '"/islem-defteri"'          → 0
UTF-16LE '"/islem-defteri"'          → 0
```
Bağlam dökümü (UTF-16LE): `…CARI_EKSTRE⼀/raporlar/islem-defteriᄁ/caril…` — gezinme dizgeleri arasında.
Bağlam dökümü (UTF-8): `…/giris … /hurda … /raporlar/islem-defteri … /islemler…` — `@page` rota tablosu.

**Sonuç:** yayınlanan yapıtta ölü `/islem-defteri` bağlantısı **yoktur**; her iki katmanda da yalnız doğru adres vardır.

### SQL kanıtı / test kiracısı
Bu paket veri yazmaz, kiracı gerektirmez → **test kiracısı açılmadı**, dolayısıyla silinecek kiracı da yok.

```
SELECT COUNT(*) FROM tenants;   -- önce:  9
SELECT COUNT(*) FROM tenants;   -- sonra: 9
```
`QA-TEST-` önekli kalıcı fikstürlere **dokunulmadı**.

API durduruldu: `lsof -nP -iTCP:5195 -sTCP:LISTEN` → **0 satır**.
**5196/5197/5198/5199/5200'e dokunulmadı.**

---

## 8. Değişen dosyalar

`git status --porcelain` (yalnız bu paketin dosyaları; `?? ` = `.gitignore` gereği izlenmeyenler):

```
 M dotnet/Kuyumcu.Web/Pages/Panel.razor
 ?? dokumanlar/09-uygulama-kayitlari/betikler/demo-tohumla.js
 ?? dotnet/Kuyumcu.Tests/OluIcBaglantiTestleri.cs
 ?? dotnet/denetim.sh
```

- `dotnet/Kuyumcu.Web/Pages/Panel.razor` — 1 satır (`href`)
- `dokumanlar/09-uygulama-kayitlari/betikler/demo-tohumla.js` — adım 8 etiketi + tek kaynak kapısı + rapor alanı
- `dotnet/denetim.sh` — P02-1b ve P02-1c sayaçları
- `dotnet/Kuyumcu.Tests/OluIcBaglantiTestleri.cs` — **yeni**, 6 test

**COMMIT YAPILMADI.** Mimari belgeye ve paket spesifikasyonuna dokunulmadı; `SONRAKI-OTURUM-PROMPT.md`ye dokunulmadı. Çalışma ağacına `.mutorj/.bak/.tmp` bırakılmadı (geçici dosyaların tamamı scratchpad altında).

---

## 9. Koşul karşılama

| Koşul | Durum | Kanıt |
|---|---|---|
| ARIZA 1 (Panel ölü bağlantı) düzeltildi | ✅ | § 1 D-b · § 7 bayt ölçümü |
| ARIZA 2 (`demo-tohumla.js` rota etiketi) düzeltildi | ✅ | § 1 D-c |
| İkisi **tek kaynaktan** üretiliyor | ✅ | `uygulamaRotalari()` + `provaRotalariniDogrula()`, `@page` tek kaynak |
| KAPI: `OLU_IC_BAGLANTI = 0` denetim sayacı | ✅ | § 6, `✓ 0 (22 bağlantı / 46 rota)` |
| Kapı **testle kilitli** | ✅ | 6 test, § 4 |
| Mutasyon: href bozulunca test KIRMIZI | ✅ | M-D1-1, M-D1-5 |
| KAPSAM KİLİDİ (yalnız 2 dosya + sayaç + test) | ✅ | § 8 — 4 dosya, başkası yok |
| Tasarım/düzen değiştirilmedi | ✅ | Panel.razor'da yalnız `href` değeri |
| Eşzamanlı şeritlerin dosyalarına dokunulmadı | ✅ | PARA/YAYIN/ŞERİT A dosyaları ve `Sozlukler.cs` **hiç açılmadı** |
| `Sozlukler.cs` düzenlenmedi | ✅ | Yeni metin gerekmedi (kullanıcıya görünen metin değişmedi) |
| İzolasyon: `KUYUMCU_TEST_DB=kuyumcu_test_d` | ✅ | her `dotnet test` çağrısında verildi |
| Canlı port 5195, 5196-5200'e dokunulmadı | ✅ | § 7 |
| Derleme 0 hata | ✅ | § 3 |
| Tam süit 0 başarısız | ❌ | § 4 — **başka şeritlerin yarım işi** (`sema` migration'sız kolon); bu paketin testleri 6/6 yeşil |
| `denetim.sh` TEMİZ | ❌ | § 6 — **P-11 48>47**, `Panel.razor` katkısı 0; artış iki koşum arasında başka şeritten geldi |
| Migration | — | gerekmedi (veri modeli değişmedi) |
| Test kiracısı silindi | — | açılmadı; `tenants` 9 → 9 |

---

## 10. Açık kalanlar

1. **`sema` kolonu migration'sız** (`Entities.cs:308`) — DB tabanlı 1730 testi düşürüyor. **Sahibi: görünüm/tema şeridi.** Bu paket kapsamı dışıdır ve o şerit migration'ı yazana dek tam süit hiçbir şeritte yeşil olamaz. **Orkestratör kapısına acil kalem.**
2. **P-11 `DIS_HATADA_VERI_DUSUREN_YOL_TOPLAM` 47 → 48** — tavan yalnız azalır kuralına aykırı artış; katkı dağılımına göre kaynak büyük olasılıkla ŞERİT A'nın `Faturalar.razor` düzenlemesi. Tavanı yükseltmek **yasak**; ilgili şerit düşürmeli.
3. **`OLU_IC_BAGLANTI` kapsamı düz metin hedeflerle sınırlı.** `href="@(...)"` / `NavigateTo($"…")` gibi enterpolasyonlu gezinmeler statik çözülemediği için sayılmaz. Bunları kapsamak ayrı bir paket işidir (çalışma zamanı rota doğrulaması ya da gezinme yardımcısına tek kapı).
4. **`/kilavuz.html` sunucuda 404** (QA turu 8'in en düşük puanlı ekranı). Sayaç bunu bilerek saymaz (statik varlık, rota değil). **Demo sırasında kenar çubuğundaki Kılavuz bağlantısı bir tık uzakta** — DEMO-SENARYOSU § 5/A zaten "açılmayacak" diyor; S-4'ün ilk üç kalemi arasında.
5. **Yayın anlık görüntüsü (Z1 / D-a)** bu paketin kapsamında değildi. Panel düzeltmesinin demoda görünmesi için **yayın anlık görüntüsü yeniden üretilmelidir** (YAYIN şeridi) — aksi hâlde 5199'daki eski yapıt hâlâ ölü bağlantıyı taşır.

---

## 11. Kurucuya / mali müşavire notlar

- **Mali müşavire not yok:** bu pakette hiçbir yasal/mevzuat hükmü yorumlanmadı veya koda yazılmadı.
- **Kurucuya, cumartesi demosu için:**
  1. Panel'deki **"Son işlemler →"** bağlantısı artık çalışıyor ve `/raporlar/islem-defteri`'ne gidiyor. DEMO-SENARYOSU § 1 / ADIM 2'deki *"bu ekranda asla tıklanmayacak 4 bağlantı"* listesinden **birinci satır (ölü bağlantı) kalktı**; diğer üçü (`/kasalar`, `/hurda`, `/vezne`) **hâlâ geçerlidir, tıklanmamalıdır**.
  2. Bu düzeltme **kodda** yapıldı. Demoda göreceğiniz yayın anlık görüntüsü yeniden üretilmezse **eski, ölü hâli çalışmaya devam eder** (Z1/D-a ayrı iş).
  3. `demo-tohumla.js` provası artık yanlış rota adıyla yeşil dönemiyor; prova raporunda `rota_tek_kaynak` alanı kanıt olarak yer alıyor.
