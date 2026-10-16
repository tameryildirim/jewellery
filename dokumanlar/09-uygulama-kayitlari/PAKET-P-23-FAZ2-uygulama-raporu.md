# PAKET P-23 — FAZ 2 UYGULAMA RAPORU
**Ödeme kalemi tablosu: geri doldurma · kapsam bayrağı · R-18 kararı · ekran**
Tarih: 2026-09-22 · Roller: Taşkın (backend) + Ertuğ (frontend) · Dalga 6
Durum: **YARIM** — üç kalem girdi, bir kalem (vezne fişi başlığının
genelleştirilmesi) girmedi ve açıkça devredildi. Ayrıntısı § 12.

> ⚠ Bu rapor Faz 1 raporunun (`PAKET-P-23-uygulama-raporu.md`) devamıdır.
> Faz 1 kodu GERİ ALINMADI; bu faz onun üzerine ekledi.

---

## 1. ÖNKOŞUL ÖLÇÜMÜ (durma-devam modunda, varsayım yok)

| Önkoşul | Diskte var mı | Ölçüm |
|---|---|---|
| P-21 (avans/mahsup) | **VAR** | `Modules/Satis/SatisModulu.P21.cs` — `AvansMahsupDto`, `AvansKapisi.Mahsup` mevcut |
| P-01 (fiş okuma) | **VAR** | `SatisModulu.FisOku` + `GET /api/v1/islemler/fis/{fisId}` |
| P-23 Faz 1 | **VAR** | `Data/OdemeKalemi.cs`, migration `20260921224711_P23OdemeKalemleri` |

Kapsam daraltılmadı; Faz 1'in bıraktığı üç açık (geri doldurma · R-18 · ekran)
hedef alındı.

---

## 2. HASIM DOĞRULAYICININ AÇIK BULGULARI — NE OLDU

| # | Bulgu (2026-09-22) | Bu fazdaki karşılık |
|---|---|---|
| KRİTİK-1 | Geri doldurma koşmadı (`odeme_kalemleri`=0) | **KAPANDI** — migration koştu, **90 kalem / 86 fiş / 10.685.696,83 TL**, BİLİNMİYOR = 0 (§ 5) |
| KRİTİK-2 | R-18 mahsup hiç girmedi | **KAPANDI (karar)** — planın "ya tümü ya hiç" kuralının **HİÇ** dalı seçildi, yapısal olarak kilitlendi + test (§ 6) |
| KRİTİK-3 | Vezne fişi başlığı genelleştirilmedi | **DEVREDİLDİ → P-23 Faz 3** (§ 12) — gerekçe yazılı |
| KRİTİK-4 | Hiçbir ekran `odemeKalemleri`'ni göstermiyor | **KAPANDI** — `/islemler` "Son satışlar" satırına ödeme kırılımı geldi + 3 bUnit testi (§ 7) |
| ORTA-1 | `FIS_TOPLAM_ODEME_KALEMI_FARKI` adının söylediğini ölçmüyor | **KAPANDI** — çalışma zamanı kapısı yazıldı, sayaç o kapıyı sayıyor (§ 8) |
| ORTA-2 | "kalem yok" ile "ödeme yok" ayrımı yalnız yorumda | **KAPANDI** — yanıtta açık `kalemKapsami` alanı (§ 4) |
| DÜŞÜK-1 | `SayisalAlan.cs` dokunuş muhasebesi | Bu turda **DOKUNULMADI**; kurucunun "adet kısmında ne yazarsam altta da o görünüyor" bulgusunun düzeltmesi kodda duruyor ve doğrulandı (§ 13) |
| DÜŞÜK-2 | 0 baytlık artıklar (20 + 8) | Bu turda **yeni artık üretilmedi**; silme kurucuda (hook `rm`'i engelliyor) |
| ORTA-3 | `docker-compose.yml` MySQL parolası uyuşmuyor | **BİREBİR YENİDEN ÜRETİLDİ** (§ 13) — runbook borcu duruyor |

---

## 3. SPESİFİKASYONUN KODA KARŞI DOĞRULANMASI (sapmalar)

| Plandaki ifade | Kodda ölçülen | Uyarlama |
|---|---|---|
| `Data/VezneFisi.cs:14-17,56,127` dosya ayak izinde | Dosya duruyor, satırlar kaymış | **Dokunulmadı** — bkz. § 12 |
| "`Down()` tabloyu düşürür, eski alanları korur" | Tabloyu düşüren **Faz 1** migration'ıdır; Faz 2 yalnız veri + kolon ekler | Faz 2 `Down()`'u **yalnız geri doldurulmuş satırları** siler ve kolonu düşürür; tatbikatta P-23'ün İKİSİ birden geri alındı, tablo düştü (§ 5.3) |
| "KARMA ödemelerin geri doldurulması" | KARMA satır sayısı **6**; ama fiş düzeyinde bakıldığında KARMA artığı **0** | Geri doldurma **fiş düzeyinde** ve **tüm** ödeme türlerinde koştu (86 fiş), yalnız KARMA'da değil — hedefe sadık, kapsamı daha dürüst |
| "belirsiz satır BİLİNMİYOR işaretlenir, sayısı raporda açık" | Ölçüldü: **0 satır** belirsiz | `OdemeAraci.Bilinmiyor` damgası ve `bilinmiyor` bayrağı yine de KOD'da duruyor; bugünkü veride tetiklenmedi |

---

## 4. KOD — KAPSAM BAYRAĞI (ORTA-2)

`Kuyumcu.Api/Data/OdemeKalemi.cs` → yeni `OdemeKalemKapsami` kapalı listesi:

| Değer | Anlamı |
|---|---|
| `KAYITLI` | Her bacak ölçülmüş bir kalem satırı |
| `BILINMIYOR` | Fişin HİÇ kalemi yok → kırılım bilinmiyor, **"ödeme yok" DEĞİL** |
| `KISMEN_BILINMIYOR` | Kalem var ama en az biri `bilinmiyor` damgalı |

`FisOku` yanıtı artık `kalemKapsami` alanını taşıyor; her kalemde
`geriDolduruldu` bayrağı da dönüyor. Kural **yorumdan koda** taşındı.

---

## 5. MIGRATION — GERİ DOLDURMA (BK-03'ün üç şartı)

**Ad:** `20260921231041_P23Faz2GeriDoldurma`
**Dosya boyutları:** `.cs` **10.950 bayt** · `.Designer.cs` **213.895 bayt** (ikisi de > 0)
**`--no-build` KULLANILMADI** — her `dotnet ef` çağrısı yeşil derlemeden sonra koştu.

### 5.1 (i) SALT OKUNUR ETKİ RAPORU — koşmadan önce ölçüldü

`hareketler` (172 satır) içinde `odeme_turu` dolu **87** satır:

| odeme_turu | satır | fis_id'si olan | nakit_tutar dolu | veresiye_tutar dolu | ikisi de boş |
|---|---|---|---|---|---|
| VERESIYE | 26 | 0 | 10 | 10 | 16 |
| NAKIT | 18 | 0 | 18 | 18 | 0 |
| KART | 13 | 0 | 13 | 13 | 0 |
| HAVALE | 24 | 0 | 23 | 23 | 1 |
| KARMA | 6 | 4 | 6 | 6 | 0 |

**Kritik ölçüm:** 87 satırın yalnız **4'ünün** `fis_id`'si var → fiş birimi
`COALESCE(fis_id, id)` olarak tanımlandı (varsayım değil, ölçüm).

Fiş düzeyinde beklenen üretim:

| Kaynak | bacak |
|---|---|
| `kasa_hareketleri` (NAKIT/KART/HAVALE, hepsi TRY) | **59** |
| `hareketler.veresiye_tutar > 0` | **12** |
| ARTIK (fiş toplamı − yukarıdaki ikisi) | **19** |
| **TOPLAM** | **90 kalem / 86 fiş** |

Artığın araç dağılımı (hepsi tek türlü fişten, araç **yazılı**):
VERESIYE 17 · HAVALE 1 · NAKIT 1 → **BİLİNMİYOR = 0**.
Toplamı aşan fiş: **0**.

> `odeme_turu` NULL olan 85 satıra (EMANET_GIRIS 22, SAYIM_DUZELTME 18,
> ALIS 14, SATIS 11, …) **DOKUNULMADI**: onların ödeme aracı hiçbir yerde
> yazılı değil, türetmek tahmin olurdu (md.1). O fişler `BILINMIYOR` kapsamıyla
> döner.

### 5.2 Koşum sonucu — beklenenle BİREBİR

```
kalem=90  geri_dolduruldu=90  bilinmiyor=0  fis=86  toplam=10685696.83
hareketler=172 (DEĞİŞMEDİ)   __EFMigrationsHistory=63   tenants=9
arac dağılımı:  NAKIT 21 / KART 15 / HAVALE 25 / VERESIYE 29
DENKLİK: 86 fişin 86'sında (fiş toplamı − kalem toplamı) = 0 → sapan 0
```

### 5.3 (ii) `Down()` TATBİKATI — GERÇEKTEN KOŞTURULDU

```
dotnet ef database update 20260921084754_P37KullaniciTercihi
  Reverting migration '20260921231041_P23Faz2GeriDoldurma'.
  Reverting migration '20260921224711_P23OdemeKalemleri'.
  Done.
SONRASI:  hareketler=172 · tenants=9 · __EFMigrationsHistory=61
          odeme_kalemleri tablosu = YOK (düştü)
          hareketler'de nakit_tutar+veresiye_tutar+odeme_turu = 3/3 KORUNDU
          ödeme taşıyan satırların toplamı = 10685696.83 (DEĞİŞMEDİ)
```

### 5.4 (iii) YEDEK + GERİ DÖNÜŞ PROVASI

| Anlık görüntü | Boyut | MD5 |
|---|---|---|
| ÖNCESİ (`yedek-ONCESI-kuyumcu_net.sql`) | 40.628.456 bayt | `2cffc57276b9cf4551ba13e5d0ec49de` |
| SONRASI (`yedek-SONRASI-kuyumcu_net.sql`) | 40.684.852 bayt | `e69b3a4b9906ab8b9df7fe4ef5cc7b8c` |

Yedekler oturum karalama dizinindedir (`…/scratchpad/`), proje ağacına
yazılmadı. Prova **veri İÇEREN** veritabanında (172 hareket, 9 kiracı) koştu.

**Tam tur:** uygula → doğrula → `Down()` → doğrula → **yeniden uygula** →
sonuç **BİREBİR AYNI** (90 / 86 / 10.685.696,83). Geri dönüş tek yönlü değil,
**tekrarlanabilir**.

### 5.5 Yarım koşum olayı — gizlenmiyor (md.1)

İlk `Up()` denemesi `only_full_group_by` yüzünden **HATA verdi**
(`Expression #9 of SELECT list is not in GROUP BY clause`). MySQL'de DDL işlem
dışı olduğu için **kolon veritabanında kaldı, history satırı yazılmadı**.
Kolonu elle düşürmek istedim; ortamın izin sınıflandırıcısı `ALTER TABLE`'ı
**engelledi** (*Modify Shared Resources*). Bunu aşmak yerine **migration'ın
kendisi korumalı yazıldı**: kolon ekleme `information_schema`'ya bakan
hazırlanmış bir deyimdir ve yarım kalmış bir veritabanında da baştan koşar;
ayrıca artakalan geri doldurulmuş satırlar `Up()`'ın başında silinir
(iki kez yazıp toplamı ikiye katlamasın). Bu, kusurdan doğan ve **kalıcı
değeri olan** bir sertleştirmedir.

---

## 6. R-18 MAHSUP — KARAR: **HİÇ** (tümü-ya-hiç kuralının ikinci dalı)

Plan: *"ya **tüm** ödeme yollarını kapsar ya **hiç** girmez — kısmi mahsup
bakiyeyi saptırır, **mimari veto**"* (§ 2 P-23 ve § 11 R-18).

**Seçilen dal: HİÇ.** Gerekçe:

1. Avans mahsubu (P-21 · `AVANS_MAHSUP`) bir ödeme **bacağı** değil, satıştan
   **sonra** yazılan ayrı bir defter hareketidir; fişin ödeme sepetine hiç
   girmez. Onu bir `OdemeAraci` yapmak, bacak toplamını fiş toplamının
   **üstüne** çıkarır ve § 8'deki toplam denkliği kapısını kırar.
2. "Tümü" dalı, mahsubun tahsilat · vezne · takas · emanet · işçilik ve P-21
   değişimi yollarının **hepsinde** kalem üretmesini gerektirir. Bu yolların
   hiçbiri bugün kalem yazmıyor (`HAREKETLERDE_ODEME_YONTEMI_YENI_YOL = 0`).
   Yarısını yapmak planın kendi ifadesiyle **mimari veto** sebebidir.

Karar **yapısal olarak kilitlendi**: `OdemeAraci.Tumu` MAHSUP içermez,
`Gecerli("MAHSUP")` = false, ve `R18_Mahsup_Odeme_Araci_Olarak_Girmez` testi
listeye sızmayı kırmızıya çevirir (mutasyon M-P23F2-03 ile kanıtlı).

> **Orkestratöre not:** bu bir *kapsam kararıdır*, bir *eksik* değil. "Tümü"
> dalı istenirse ayrı bir paket (tüm ödeme yollarında kalem yazımı) gerekir;
> tek başına mahsup eklenemez.

---

## 7. EKRAN — `/islemler` "Son satışlar" ödeme kırılımı (KRİTİK-4)

- Her satırın altına **"Ödeme kırılımını göster"** düğmesi geldi
  (`islem-kirilim-{hareketId}`). Düğme **her zaman gerçek istek atar**
  (DoD EK md.1) — `GET /islemler/fis/{id}`.
- Açılan gövde (`islem-kirilim-govde-{id}`): kapsam bandı
  (`islem-kirilim-kapsam`), kalem listesi (`islem-kirilim-liste`), boş durum,
  hata bandı.
- **UX-S28:** `catch` içinde liste boşaltılıp "kayıt yok" GÖSTERİLMEZ; hata
  bandı ayrıdır, boş durum yalnız istek başarılı ve sonuç gerçekten boşken çıkar.
- **UX-S31:** ham kod ekrana basılmaz — `NAKIT` → "Nakit", `KAYITLI` →
  "Kırılım kayıtlı"; hepsi `Sozlukler.cs`'ten (TR + EN eklendi).
- **UX-S32:** tutar `Bicim.Para(tutar, paraBirimi)` ile, para birimiyle birlikte.
- Geri doldurulmuş satır **"Geçmiş kayıttan türetildi"** rozetiyle işaretli —
  kaynağı gizlenmiyor.
- **Yeni istemci record AÇILMADI:** ortak tip `Kuyumcu.Contracts/Sayfalar/
  IslemlerSozlesmeleri.cs` → `OdemeKalemiSatiri` (ad + tip sunucuyla eşleşir).
- Sahte bUnit API'sine `/islemler/fis/` için **gerçekçi** yanıt eklendi
  (`{}` varsayılanı bırakılmadı).

**Sunucu tarafı yardımı:** `FisOku` artık **hareket kimliğini de** kabul eder
(hareketin `fis_id`'si varsa fişin tamamı, yoksa hareketin kendisi). Sebebi
ölçümdür: ödeme taşıyan 87 hareketin 83'ünün `fis_id`'si yok; bu olmadan geri
doldurulan kırılım ekranda **hiç görünmezdi**.

**Dokunulan sayfalar (TEK DOKUNUŞ muhasebesi):** `Kuyumcu.Web/Pages/Islemler.razor`
— bu dalgada ilk dokunuş bu pakette.

---

## 8. SAYAÇLAR (mekanizma kuralı) — tabanlar TAZE ölçüldü

| Sayaç | Değer | Ölçüm |
|---|---|---|
| `HAREKETLERDE_ODEME_YONTEMI_YENI_YOL` | **0** | `grep -rn 'OdemeKalemleri\.Add' Kuyumcu.Api` − `SatisModulu.cs` → 0 (değişmedi) |
| `FIS_TOPLAM_ODEME_KALEMI_FARKI` | **0** | **ÖLÇÜMÜ DEĞİŞTİ** (aşağıda) |

### İkinci sayacın düzeltilmesi (ORTA-1)

Eski ölçüm, `OdemeKalemleri.Add` satırının 40 satır öncesinde `sepet.Bacaklar`
geçip geçmediğine bakan bir **awk pencere taramasıydı**: hiçbir toplam
karşılaştırmıyordu ve pencere kırılgandı. Rapor okuyanı "toplamlar
denkleşiyor" sanısına düşürüyordu.

**Artık farkı KOD ölçüyor.** `SatisModulu.SatisFisi`, kalemleri veritabanına
eklemeden önce:

```csharp
var kalemToplam = Yuvarla2(yazilanKalemler.Sum(k => k.TutarTl));
if (kalemToplam != Yuvarla2(fisToplam))
    throw new ConflictException(… "Fiş yazılmadı.");
db.OdemeKalemleri.AddRange(yazilanKalemler);
```

Sayaç bu kapının **sökülmediğini** sayar (kapı + AddRange + testin varlığı).
Tavan **artırılmadı**; ölçüm komutu betiğe yorum olarak yazıldı.

> Ayrıca `Pages: @onclick'siz ve disabled'sız <button>` tavanı (71) yeni
> düğmeyle geçici olarak 72'ye çıkmıştı; **tavan yükseltilmedi**, işaretleme
> aynı satıra alınarak sayaç 71'e geri indirildi.

---

## 9. TESTLER

| Koşum | Toplam | Başarılı | Başarısız |
|---|---|---|---|
| Filtreli (P23 + UX1C1) | 56 | 56 | 0 |
| **Tam süit** | **3785** | **3785** | **0** |

Önce: 3774 · Sonra: **3785** → **+11 yeni test**.

Yeni dosya: `Kuyumcu.Tests/P23Faz2Testleri.cs` (8 test)
`Kuyumcu.Tests/UX1C1EkranTestleri.cs` (+3 bUnit testi)

| Test | Ne sınıyor |
|---|---|
| `R18_Mahsup_Odeme_Araci_Olarak_Girmez` | R-18 kararı (§ 6) |
| `Kapsam_kalem_yoksa_BILINMIYOR_der` | "kalem yok ≠ ödeme yok" |
| `Kapsam_kalem_varsa_KAYITLI_damgali_kalem_varsa_KISMEN_der` | üç durumlu kapsam |
| `Kapsam_listesi_KAPALIDIR_ucten_fazla_deger_yoktur` | sihirli dize engeli |
| `Fis_okumasi_kalemKapsami_KAYITLI_doner` | yanıt alanı (DB) |
| `Kalemi_olmayan_eski_fiste_kapsam_BILINMIYOR_doner` | eski yol + bayrak |
| `Geri_doldurulmus_BILINMIYOR_damgali_kalem_kapsami_KISMEN_yapar` | damga → kapsam |
| `Toplam_denkligi_kapisi_fisi_YAZAR_ve_kalem_toplami_fis_toplamina_ESITTIR` | § 8 kapısı |
| `P23_odeme_kirilimi_dugmesi_GERCEK_istek_atar` | DoD EK md.1 |
| `P23_kirilim_kalemleri_ARAC_ADIYLA_ve_para_birimiyle_yazilir` | UX-S31/S32 |
| `P23_kapsam_bayragi_ekranda_TURKCE_yazar_ham_kod_gorunmez` | UX-S31 |

---

## 10. MUTASYON KANITLARI

| Kod | Değişiklik | Kırmızıya dönen test(ler) | Geri alma |
|---|---|---|---|
| M-P23F2-01 | `OdemeKalemi.cs`: `kalemSayisi <= 0` → `kalemSayisi < 0` | `P23Faz2SafTestleri.Kapsam_kalem_yoksa_BILINMIYOR_der` · `P23Faz2Testleri.Kalemi_olmayan_eski_fiste_kapsam_BILINMIYOR_doner` (2 kırmızı) | `cmp` ✔ |
| M-P23F2-02 | `SatisModulu.cs`: `!= Yuvarla2(fisToplam)` → `!= Yuvarla2(fisToplam) + 1m` | 13 kırmızı; adıyla: `Toplam_denkligi_kapisi_fisi_YAZAR_ve_kalem_toplami_fis_toplamina_ESITTIR`, `P23OdemeKalemiTestleri.Kalem_toplami_fis_toplamina_ESITTIR`, `Fis_okumasi_kalemKapsami_KAYITLI_doner` … | `cmp` ✔ |
| M-P23F2-03 | `OdemeKalemi.cs`: `Tumu`'ya `"MAHSUP"` eklendi | `R18_Mahsup_Odeme_Araci_Olarak_Girmez` · `OdemeAraciTestleri.Arac_listesi_OdemeBacagiDto_regexi_ile_AYNIDIR` (2 kırmızı) | `cmp` ✔ |
| M-P23F2-04 | `Islemler.razor`: `Api.GetAsync<…>("/islemler/fis/…")` → `new JsonElement()` | 3 kırmızı: `P23_odeme_kirilimi_dugmesi_GERCEK_istek_atar`, `P23_kirilim_kalemleri_…`, `P23_kapsam_bayragi_…` | `cmp` ✔ |

Test açığı bulunmadı. Son mutasyondan sonra derleme: **0 uyarı / 0 hata**.

---

## 11. CANLI DOĞRULAMA (yalnız 5198 — **5199'a DOKUNULMADI**)

Test kiracısı: **`PAKETP-23F2-TEST`** (`01M334F30GY07RQ20SRBACCM0N`).
`QA-TEST-*` fikstürlerine dokunulmadı.

| # | Adım | Beklenen | Gerçek | Kanıt |
|---|---|---|---|---|
| 1 | `GET /islemler/fis/deneme` jetonsuz | 401 | **401** | kimlik kapısı fail-closed |
| 2 | `POST /sistem/davetler` gerekçesiz | 400 | **400** | *"Gerekçe zorunludur"* |
| 3 | `POST /sistem/davetler` gerekçeli | 201 | **201** | jeton maskelendi; anahtar yalnız oturum ortam değişkeninde üretildi, hiçbir dosyaya yazılmadı |
| 4 | `POST /auth/kayit` davetli | 201 | **201** | tenant `01M334F30G…` |
| 5 | kasa / ürün / cari / alış (stok) | 200-201 | **200 · 201 · 201 · 200** | stok 5 adet |
| 6 | `POST /islemler/satis-fisi` (NAKIT 4.000 + VERESIYE 6.000 = 10.000) | 200 | **200** | `fisId=01M334FX8YC4ZRYAB2S6ENCK61` |
| 7 | `GET /islemler/fis/{fisId}` | 200 + `kalemKapsami` | **200** | `kalemKapsami=KAYITLI`, `odemeKalemToplam=10000.0`, 2 kalem |
| 8 | `GET /islemler/fis/{hareketId}` (yeni geri düşüş) | 200, aynı fiş | **200** | `fisId` doğru çözüldü, 2 kalem |
| 9 | `GET /islemler/fis/{ALIS hareketId}` (kalemi YOK) | 200 + `BILINMIYOR` | **200** | `kapsam=BILINMIYOR`, kalem 0, `bacaklar` 1 → **eski yol sağlam** |
| 10 | `GET /islemler/fis/01YOK…` | 404 | **404** | |
| 11 | SQL: kalem satırları | 2, VERESIYE dâhil | **2** | aşağıda |
| 12 | SQL: fiş toplamı = kalem toplamı | eşit | **10000.00 = 10000.00** | aşağıda |
| 13 | Temizlik | tenants 9'a döner | **9** | aşağıda |

```
sira arac      tutar_tl para_birimi kasa bilinmiyor geri_dolduruldu aktif
0    NAKIT     4000.00  TRY         1    0          0               1
1    VERESIYE  6000.00  TRY         0    0          0               1

fis_toplam = 10000.00    kalem_toplam = 10000.00
```

**Temizlik kanıtı** (`tenant_id` taşıyan 63 tablo + `kiraci_davetleri` + `tenants`):

```
tenants               = 9    (başlangıçtaki değer)
odeme_kalemleri       = 90   (yalnız GERİ DOLDURULMUŞ geçmiş satırlar)
hareketler            = 172  (dokunulmadı)
__EFMigrationsHistory = 63
lsof -nP -iTCP:5198 -sTCP:LISTEN → BOŞ (API durduruldu)
```

---

## 12. § KİMLİK KAPANIŞ TABLOSU

| Kimlik | Durum | Kanıt |
|---|---|---|
| **R-17** Ödeme kalemi tablosu | **KAPANDI** | Faz 1 (tablo + ileri yazım) + Faz 2 (geri doldurma 90 kalem / 86 fiş, `kalemKapsami`, ekran). Kanıt: `Data/OdemeKalemi.cs`, migration `20260921231041`, § 5, § 7, § 11 adım 7-9 |
| **R-18** Mahsup | **KAPANDI (karar: HİÇ)** | `Data/OdemeKalemi.cs` `OdemeAraci.Tumu` gerekçeli blok; test `R18_Mahsup_Odeme_Araci_Olarak_Girmez`; mutasyon M-P23F2-03. Planın izin verdiği iki daldan biri seçildi ve yapısal olarak kilitlendi (§ 6) |
| **Vezne fişi başlığının genelleştirilmesi** (paket adının ikinci yarısı; kimlik numarası yok) | **DEVREDİLDİ → P-23 Faz 3** | `Data/VezneFisi.cs` bu turda **DEĞİŞTİRİLMEDİ**. Gerekçe: başlığın genelleştirilmesi vezne fişini satış dışı ödeme yollarına açar; bu, `HAREKETLERDE_ODEME_YONTEMI_YENI_YOL = 0` sayacının beyaz listesini genişletmeyi ve tahsilat/takas/emanet/işçilik yollarının hepsinde kalem yazımını gerektirir — yani R-18'in "tümü" dalıyla **aynı** işi. Bu faz, kapsam kilidi gereği (yeni ekran/modül açma, yaz-seri) o kapsamı açmadı |

---

## 13. AÇIK KALANLAR VE KURUCUYA / MALİ MÜŞAVİRE NOTLAR

1. **P-23 Faz 3 (yeni paket önerisi):** vezne fişi başlığının genelleştirilmesi
   + tüm ödeme yollarında (tahsilat · vezne · takas · emanet · işçilik · P-21
   değişimi) kalem yazımı + sayacın beyaz listesinin genişletilmesi. R-18'in
   "tümü" dalı istenirse **aynı** pakette yapılmalıdır.
2. **Runbook borcu (üçüncü kez doğrulandı):** `docker-compose.yml` içindeki
   `MYSQL_ROOT_PASSWORD` çalışan konteynere **uymuyor**
   (`ERROR 1045 (28000): Access denied`). Çalışan kimlik
   `appsettings.Development.json` bağlantı dizesindedir; bu turdaki tüm DB
   doğrulamaları onunla yapıldı. `kurallar/RUNBOOKS.md` ve `docker-compose.yml`
   hizalanmazsa her yeni ajan turu aynı duvara çarpar.
3. **İzin sınırı (yeni):** ortam, paylaşılan veritabanında elle `ALTER TABLE`
   çalıştırılmasını engelliyor (*Modify Shared Resources*). Bu turda çözüm
   migration'ı korumalı yazmak oldu; ama **yarım kalmış bir migration'ı elle
   temizleme yolu kapalı**. Orkestratör bunu bir çalışma kuralı olarak yazsın:
   *veri yazan her migration, yarım kalmış veritabanında yeniden koşabilecek
   biçimde korumalı yazılır.*
4. **0 baytlık artıklar:** bu turda **yeni artık üretilmedi**. Önceki
   dalgalardan kalan 20 dosya (`*.mutorj`, `*.mutbak`, `*.bak`) + 8 boş
   `FiyatListesiVeKampanya` migration iskeleti duruyor; hook `rm`'i engellediği
   için **silme kurucudadır**.
5. **`SayisalAlan.cs` dokunuş muhasebesi:** kurucunun bildirdiği *"adet kısmında
   ne yazarsam altta da o görünüyor"* hatasının düzeltmesi
   (`Kuyumcu.Web/Bilesenler/Form/SayisalAlan.cs` → `BicimBilgiEkliyor` ölçütü
   rakam dizisi yerine **sayısal değer** karşılaştırması) kodda duruyor,
   okundu ve doğrulandı; bu turda **dokunulmadı**. Hangi dalganın yazdığı
   kanıtlanamıyor — tahmin edilmiyor (md.1); orkestratör kapı kaydında
   netleştirsin.
6. **Mali müşavire:** bu fazda **hiçbir mevzuat hükmü koda sabit yazılmadı**.
   TevsikKapisi (459 s. VUK) ve MASAK kümülatifi **eski kaynaklarından**
   okumaya devam ediyor; `odeme_kalemleri` onların yanında ikinci bir kayıt
   olarak duruyor. Kaynak değişimi yapılmadı → **gerileme yok** (3785/3785).
   Kaynak değişimi ayrı bir karar ve ayrı bir pakettir.
7. **Bütçe:** plan tahmini 600 ajan-dk (Faz 1 + Faz 2). Faz 2 alt-adım kırılımı
   (Mustafa K-F7): ölçüm ~35 dk · migration + prova ~70 dk · kod (kapsam +
   kapı + R-18) ~45 dk · ekran + sözlük ~55 dk · test + mutasyon ~50 dk ·
   canlı doğrulama ~45 dk · rapor ~30 dk. Kalan iş (vezne + tüm yollar)
   Faz 3'e devredildi.

---

## 14. DEĞİŞEN DOSYALAR (bu tur)

```
dotnet/Kuyumcu.Api/Data/OdemeKalemi.cs
dotnet/Kuyumcu.Api/Modules/Satis/SatisModulu.cs
dotnet/Kuyumcu.Api/Data/Migrations/20260921231041_P23Faz2GeriDoldurma.cs          (YENİ)
dotnet/Kuyumcu.Api/Data/Migrations/20260921231041_P23Faz2GeriDoldurma.Designer.cs (YENİ)
dotnet/Kuyumcu.Api/Data/Migrations/KuyumcuDbContextModelSnapshot.cs
dotnet/Kuyumcu.Core/I18n/Sozlukler.cs
dotnet/Kuyumcu.Contracts/Sayfalar/IslemlerSozlesmeleri.cs
dotnet/Kuyumcu.Web/Pages/Islemler.razor
dotnet/Kuyumcu.Tests/P23Faz2Testleri.cs                                           (YENİ)
dotnet/Kuyumcu.Tests/UX1C1EkranTestleri.cs
dotnet/denetim.sh
```

COMMIT YAPILMADI (kurucu kuralı). Hiçbir git komutu çalıştırılmadı.

---

## 15. KOŞUL KARŞILAMA TABLOSU

| Koşul | Durum | Kanıt |
|---|---|---|
| Plan bloğu okundu, sözleşmeye uyuldu | KARŞILANDI | § 1, § 3 |
| BK-03 (i) salt okunur etki raporu | KARŞILANDI | § 5.1 |
| BK-03 (ii) koşturulmuş `Down()` çıktısı | KARŞILANDI | § 5.3 |
| BK-03 (iii) yedek + geri dönüş provası | KARŞILANDI | § 5.4 (MD5'li, veri içeren DB) |
| Vezne deftere YAZMAZ kuralı korundu | KARŞILANDI | `VezneFisi.cs` değiştirilmedi |
| R-18 kısmi uygulanmadı | KARŞILANDI | § 6 — "hiç" dalı, testle kilitli |
| Storno kalemleri de döndürür | KARŞILANDI | Faz 1 `StornoServisi.cs:228` korundu, süit yeşil |
| MASAK + VUK gerilemesi yok | KARŞILANDI | kaynak değişmedi, 3785/3785 |
| Belirsiz satır BİLİNMİYOR | KARŞILANDI | mekanizma var; bugünkü veride **0 satır** |
| Y-24 taksit planı yeri korundu | KARŞILANDI | `TaksitAdedi` + `PosTahsilatId` yerinde |
| Alt-adım kırılımı (K-F7) | KARŞILANDI | § 13.7 |
| Her düğme gerçek istek atar | KARŞILANDI | § 7 + `P23_odeme_kirilimi_dugmesi_GERCEK_istek_atar` |
| Sayaçlar eklendi/güncellendi, tavan artmadı | KARŞILANDI | § 8 |
| `dotnet build` 0 hata 0 uyarı | KARŞILANDI | § 9 |
| Tam süit 0 başarısız | KARŞILANDI | 3785/3785 |
| `denetim.sh` TEMİZ | KARŞILANDI | exit 0, "DENETİM TEMİZ." |
| 5198'de canlı doğrulama | KARŞILANDI | § 11 |
| 5199'a dokunulmadı | KARŞILANDI | yalnız 5198 |
| Test kiracısı silindi, `QA-TEST-*` korundu | KARŞILANDI | tenants 9 |
| `dotnet ef` `--no-build` kullanılmadı | KARŞILANDI | her çağrı `Build succeeded.` |
| Sır/parola çıktıya yazılmadı | KARŞILANDI | tüm sırlar değişkende, jeton maskelendi |
| COMMIT yok, `rm`/`mv` yok | KARŞILANDI | git komutu çalıştırılmadı |
| **Paketin TAMAMI bitti** | **KARŞILANMADI** | vezne başlığı devredildi — § 12 |
