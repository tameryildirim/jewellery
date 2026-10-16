# PAKET Q9-4 — Çıktı belgesi: kalem tutarı, has gram biçimi, tek H1 — UYGULAMA RAPORU

Tarih: 2026-09-24 · Rol: Taşkın (backend) + Ertuğ (frontend) · Dalga: DEMO DÜZELTME (QA turu 9)
Ortam: API `dotnet run --no-build` → `http://localhost:5174`, vekil `inceleme-vekili.py` → 5173, kendi sekmem (tab-33),
test veritabanı `kuyumcu_test_l`, test kiracısı `PAKETQ9-4-TEST Kuyumcu` (API `/auth/kayit` + tek kullanımlık davet; iş sonunda silindi).
Durum: **BİTTİ** — kapı maddelerinin yedisi de sağlandı (aşağıda kanıtlarıyla).

## 1. Bulgu yeniden üretimi (kod yazmadan önce, gerçek tarayıcı, 1280 px)

| QA-9 bulgusu | Kendi ölçümüm (değişiklikten önce) |
|---|---|
| Kalem tutarı ham `45000.00` | `/cikti/SATIS_FISI/01M399BJ…` kalem satırı: `Satış \| 22 Ayar Burma Bilezik \| -13.923 \| 45000.00`; aynı sayfada toplam `₺45.000,00` |
| Müşteri fişinde eksi has gram, iki ayrı biçim | kalem `-13.923` (nokta, 3 hane) · toplam `-13,9232 has` (virgül, 4 hane). Alış fişinde kalem `-80000.00`, toplam `-₺80.000,00` |
| Çift H1 + tekrar eden H2 | başlık sırası: `H1:Belge` (üst bar) · `H2:SATIŞ FİŞİ` (şerit) · `H1:SATIŞ FİŞİ` (belge). Gider pusulasında da 2 H1 |
| Belge no 26 karakterlik iç kimlik | `Belge no 01M399BJ245HZETR0TE5RXSJGS` |

SQL (aynı kayıtlar): `hareketler` → `SATIS 45000.00 / has -13.923200`, `ALIS -80000.00 / has 27.846400`, `HURDA_ALIS -23564.10 / 9.618000` (defterdeki iç işaret doğru; sorun kâğıda ham basılmasıydı).

Kök neden: sunucu kalem hücrelerini `InvariantCulture` dizgesi olarak gönderiyordu (bu doğru — biçim istemcinin işi), ama istemci **hangi hücrenin para, hangisinin has olduğunu bilmiyordu** ve hücreyi olduğu gibi basıyordu. Has gram 3 hane dizildiği için toplamla (Bicim.Has, 4 hane) tutarsızdı; defterin iç işareti de kâğıda taşınıyordu.

## 2. Yapılanlar

1. **Sözleşme** (`Kuyumcu.Contracts/Cikti/CiktiSozlesmeleri.cs`): kapalı liste `SutunBicimi {METIN, HAS, PARA}`; `CiktiBolum.SutunBicimleri` (isteğe bağlı, null = eski davranış), `CiktiSatir.ParaBirimi` (UX-S32: birim tutarla taşınır), `OrtakBelge.BelgeNoKayitKimligi`. Hepsi sona eklenen varsayılanlı parametreler — geriye uyumlu.
2. **Sunucu** (`Kuyumcu.Api/Modules/Cikti/CiktiModulu.cs`):
   - Hareket belgelerinde (fiş / makbuz / emanet) kalem has ve tutarı **işaretsiz**, has **4 hane** dizilir; yön zaten "Tür" sütunundadır (Satış/Alış…).
   - Toplam satırı `ToplamSatiri(...)`: tek yönlü fişte "Toplam tutar / Toplam has gram" + mutlak değer; **karışık yönlü** fişte (ör. satış + bozdurma alışı) net değer mutlak, yön etikette: "Net tutar — müşteriye ödenen", "Net has gram — müşteriden alınan". Eksi işareti kâğıda hiçbir durumda çıkmaz, yön bilgisi de kaybolmaz.
   - Cari ekstresinde işaret **korunur** (bakiye yönü anlamlıdır), yalnız biçim birleşti (has 4 hane, tutar ₺).
   - `BelgeNoKayitKimligi = true` (hareket ve ekstre belgelerinde numara sistem kaydı kimliğidir).
3. **Şablon** (`Bilesenler/Yazdirma/A4Belge.razor`): `Hucre(bolum, satir, i)` → HAS: `Bicim.Has` (toplamla birebir aynı biçimleyici), PARA: `Bicim.Para(tutar, satırın birimi)`; birimi olmayan tutara ₺ konmaz. Biçim sabit tr-TR — İngilizce tarayıcı/dil `TRY6,754.91` üretemez. Sayı sütunları sağa yaslı ve tek satır (mevcut `.belge-sag` + `.hucre-tek-satir`), başlık tek satır (`.hucre-tek-satir`), kayıt numarası gerektiğinde kırılır (`.kir-uzun`). Belge no altında yalnız ekranda (yazdirma-gizle) "Bu numara sistemdeki kayıt numarasıdır; ayrı bir fiş sıra numarası tutulmuyor."
4. **Tek H1**:
   - `Pages/Cikti.razor`: şerit başlığı belge adını ikinci kez yazmaz; "Yazdırma önizlemesi" der ve başlık öğesi çizmez (`SayfaBasi BaslikOgesiYok="true"`).
   - `Bilesenler/SayfaBasi.razor`: yeni isteğe bağlı parametre `BaslikOgesiYok` (varsayılan false → 47 ekranın davranışı değişmez); true ise aynı sınıf/id ile `<p>`.
   - `Layout/UstBar.razor`: `BelgeSayfasi(yol)` — `/cikti/…` ve `/gider-pusulalari/{Id}/belge` rotalarında rota adı `<p id="ust-bar-rota-adi" class="sayfa-basi-baslik">` (aynı görünüm, mevcut belirteç sınıfı), diğer tüm rotalarda `<h1>` aynen.
5. **Gider pusulası belgesi** (`Pages/PusulaBelgesi.razor`, yalnız sınıf/`<br />` ekleri): 375 px ölçümünde başlık iki satıra kırılıyor, "Has gram" başlığı kırılıyor, bitişik "Yazıyla" tutarı etiketin üstüne biniyordu → `.hucre-tek-satir` / `.kir-uzun` + tutar kendi satırında. Yeni CSS yok, yeni satır içi style yok.
6. **Sözlük** `Kuyumcu.Core/I18n/Sozlukler.CiktiBelge.cs` (TR + EN, konumsal record — eksik anahtar derleme hatası). Hub'a dokunulmadı.
7. **Mevcut test uyarlaması**: `RotaBaslikTestleri.Ust_bar_GERCEK_rotada_gercek_basligi_cizer` seçicisi `header.ust-bar h1` → `header.ust-bar h1, header.ust-bar #ust-bar-rota-adi` (niyet aynı: her rotada başlık dolu). Belge rotasında h1 artık bilinçli olarak yok.

## 3. Sapmalar ve gerekçeleri

- **DOSYA ALANI dışı üç dokunuş** (gerekçeli): `Layout/UstBar.razor`, `Bilesenler/SayfaBasi.razor`, `Pages/PusulaBelgesi.razor`. Kabul ölçütü "H1 sayısı 1" ve üst bar her sayfada `<h1>` çizdiği için üst bara dokunmadan sağlanamazdı. Şerit başlığını elle çizmek denemesi `denetim.sh` P-37 `SAYFA_BASI_DISI_DUZEN` ve P-37c `SAYFA_BASI_KAPSAM` sayaçlarını kırdı (ölçüldü: `elle=1`, `acik=1`) → geri alındı, SayfaBasi'ye varsayılanı değişmeyen bir parametre eklendi. PusulaBelgesi kabulde (GIDER_PUSULASI) adıyla geçiyor ve üst bar değişikliğiyle "değişen ekran" oldu; 375 px ölçümü orada 2 kırılma + 1 binme buldu, yalnız mevcut sınıflarla düzeltildi. ESZAMANLI ŞERİT listesindeki hiçbir dosyaya ve `app.css`'e dokunulmadı.
- **Belge no**: veri modelinde okunur bir fiş sıra numarası YOK (`hareketler` tablosunda sütun yok; `fis_id` de ULID). Sıra numarası uydurulmadı; ekranda numaranın ne olduğu yazıyor, kâğıtta kimlik aynen duruyor. Sıralı belge numaralandırması VUK belge düzeni konusudur — bkz. § 14.
- **Belge no ve pusula biçimi dokunulmadı**: pusulada tutarlar zaten tr-TR (`24.045,00 ₺`), has 3 hane (`9,618`) — belge kendi içinde tek biçimde; VUK md.234 belgesinin ondalık düzenini bu pakette değiştirmedim (kapsam: /cikti). Stopaj satırındaki `− 480,90 ₺` bir kesinti göstergesidir, eksi has değildir.

## 4. Migration
Gerekmedi (şema değişikliği yok; yalnız JSON sözleşmesine isteğe bağlı alanlar eklendi).

## 5. Derleme
`dotnet build Kuyumcu.sln --nologo` → **0 Uyarı, 0 Hata** (son derleme). Ara derlemelerde görülen tek uyarı `EtiketEkranTestleri.cs(145,47) xUnit1031` başka şeride ait, son derlemede yoktu.

## 6. Testler

| Koşum | Toplam | Geçti | Kaldı |
|---|---|---|---|
| Başlangıç tam süit (değişiklikten önce, `--no-build`) | 5113 | 5113 | 0 |
| Filtre `Q94` | 33 | 33 | 0 |
| Filtre `Q94\|P17\|S25b\|S25Yazdir\|S1cUstBar\|RotaBaslik\|PusulaEkran\|MarkaAdi\|MobilParite\|SayfaBasi` | 226 | 226 | 0 |
| Filtre `Q94\|Pusula` (son sınıf ekleri sonrası) | 89 | 89 | 0 |
| **Son tam süit** (`trx-pQ9-4-son`, `-v n --logger trx`) | **5146** | **5146** | **0** |

Yeni testler (`Kuyumcu.Tests/Q94CiktiBelgeTestleri.cs`, 33 koşum):
- SAF `Q94SafTestleri`: TL biçimi, has 4 hane = toplam biçimleyici, USD birimi, birimsiz tutarda ₺ yok, metin/sayı olmayan hücre, eski sözleşme geriye uyum, **en-US kültürde de** `₺45.000,00`, toplam tek yön / karışık yön, belge rotası (7 durum), sözlük TR+EN.
- HTTP `Q94HttpTestleri` (gerçek uç + kuyumcu_test_l): satış fişi işaretsiz + sütun biçimli + birim + bayrak; alış fişinde eksi TL yok; karışık yönlü fişte net ve yön etiketi; ekstrede işaret korunur.
- bUnit `Q94EkranTestleri`: `₺45.000,00` ve `45000.00`/`TRY` yok; has işaretsiz ve kalem = toplam; sayı sütunu sağa yaslı; **tek h1** + `SATIŞ FİŞİ` başlıklı h2 yok + şerit `P` ve eylemler yerinde; belge no açıklaması ekranda, `yazdirma-gizle`, başlık satırının dışında.
- bUnit `Q94UstBarTestleri`: belge rotasında h1 yok / pusula belgesinde h1 yok / diğer rotada h1 aynen.
- bUnit `Q94SayfaBasiTestleri`: varsayılan h2 (davranış değişmez) / `BaslikOgesiYok` → aynı id + sınıfla `p`.
- bUnit `Q94SigmaKilidiTestleri`, `Q94CiktiSigmaTestleri`: 375 px sığma düzeltmelerinin DOM kilidi.
- S2-5b `?yazdir=1`: mevcut `S25bSaticiYazdirTestleri` ve `S25YazdirParaUstuTestleri` yeşil (filtre koşumunda); canlıda da doğrulandı (§ 8).

## 7. Mutasyon kanıtları
Yöntem: kaynak `scratchpad/mut-yedek/Q9-4/` altına kopya → `perl -0777 -pi` TEK eşleşme (eşleşme ≠ 1 ise betik durur) → `dotnet build` + filtreli test → yedek geri → `cmp`. Son mutasyondan sonra temiz `dotnet build Kuyumcu.sln` = 0 Hata.

| Kod | Değişiklik | Kırmızıya dönen test(ler) | cmp |
|---|---|---|---|
| M94-1 | PARA → ham hücre | Q94_para_sutunu_TL_bicimiyle…, Q94_ingilizce_kulturde…, Q94_para_birimi_satirla…, Q94_kalem_tutari_TL_bicimiyle_ham_45000_00_YOK | AYNI |
| M94-2 | HAS → ham hücre | Q94_has_sutunu_virgul_ve_4_hane…, Q94_ingilizce_kulturde…, Q94_has_gram_ISARETSIZ_ve_kalem_toplam_AYNI_bicim | AYNI |
| M94-3 | kalem has `Math.Abs` kaldırıldı | Q94_satis_fisi_kalemi_ISARETSIZ…, Q94_karisik_yonlu_fiste… | AYNI |
| M94-4 | kalem TL `Math.Abs` kaldırıldı | Q94_alis_fisinde_eksi_TL_kagida_cikmaz, Q94_karisik_yonlu_fiste… | AYNI |
| M94-5 | toplam `Math.Abs(net)` → `net` | Q94_toplam_karisik_yonde…, Q94_toplam_tek_yonde…, Q94_alis_fisinde…, Q94_satis_fisi_kalemi…, Q94_karisik_yonlu_fiste… | AYNI |
| M94-6 | karışık yön algısı `false` | Q94_toplam_karisik_yonde_NET…, Q94_karisik_yonlu_fiste_NET… | AYNI |
| M94-7 | kalem has 4 → 3 hane | Q94_alis_fisinde…, Q94_satis_fisi_kalemi… | AYNI |
| M94-8a | SayfaBasi `@if (BaslikOgesiYok)` → `false` | Q94_SayfaBasi_baslik_ogesi_yoksa…, Q94_sayfada_TEK_h1… | AYNI |
| M94-8b | Cikti `BaslikOgesiYok="false"` | Q94_sayfada_TEK_h1_belge_basligi_ve_tekrar_eden_h2_YOK | AYNI |
| M94-9 | UstBar `/cikti/` → `return false` | Q94_belge_rotasi_dogru_taninir (2 durum), Q94_belge_rotasinda_ust_bar_h1_CIZMEZ… | AYNI |
| M94-10 | belge no açıklaması `@if (false)` | Q94_belge_no_kayit_numarasi_oldugu_EKRANDA… | AYNI |
| M94-11 | ekstre has `Math.Abs` eklendi | Q94_cari_ekstresinde_isaret_KORUNUR… | AYNI |
| M94-12 | hareket belgesi bayrağı `false` | Q94_satis_fisi_kalemi_ISARETSIZ_ve_sutun_bicimli | AYNI |
| M94-13 | `SayiSutunu` → `false` | Q94_eski_sozlesme…, Q94_sayi_sutunlari_saga_yaslanir… | AYNI |
| M94-14 | birimsiz tutar dalı `when false` | Q94_birimsiz_tutar_TL_simgesi_ALMAZ | AYNI |
| M94-15 | A4Belge h1 `.hucre-tek-satir` kaldırıldı | Q94_belge_basligi_tek_satir_numara_kirilabilir… | AYNI |
| M94-16 | sayı sınıfından `.hucre-tek-satir` kaldırıldı | Q94_belge_basligi_tek_satir_numara_kirilabilir… | AYNI |
| M94-17 | pusula `<br />` kaldırıldı | Q94_pusula_basligi_tek_satir_tutar_yaziyla… | AYNI |
| M94-18 | pusula h1 sınıfı kaldırıldı | Q94_pusula_basligi_tek_satir_tutar_yaziyla… | AYNI |

Test açığı: 0 (19 mutasyonun 19'u kırmızı). (İlk turdaki M94-8, şerit elle çizildiği sürümü hedefliyordu ve o da kırmızıydı; o sürüm denetim sayaçları yüzünden geri alındığı için M94-8a/8b ile yenilendi.)

## 8. Canlı doğrulama (5174 + vekil 5173, gerçek tarayıcı)

### 8.1 API (yanıtlar okundu)
| İstek | Kod | Yanıt (özet) |
|---|---|---|
| `GET /cikti/SATIS_FISI/…RXSJGS` (SATIS) | 200 | `sutunBicimleri [METIN,METIN,HAS,PARA]`, kalem `["Satış","22 Ayar Burma Bilezik","13.9232","45000.00"]` birim TRY; `Toplam tutar 45000 TRY`, `Toplam has gram 13.9232`; `belgeNoKayitKimligi true` |
| `GET /cikti/SATIS_FISI/…ASCWDH` (ALIS) | 200 | kalem `"27.8464","80000.00"`; `Toplam tutar 80000` (eksi yok) |
| `GET /cikti/CARI_EKSTRE/<cari>` | 200 | 5 sütun biçimi; satırlar `27.8464 / -80000.00` ve `-13.9232 / 45000.00` (ekstrede işaret korunur); `TL bakiye -35000` |
| `GET /cikti/SATIS_FISI/<HURDA_ALIS>` | 400 | "Bu hareket 'SATIS_FISI' belgesi üretmez (hareket türü: HURDA_ALIS)…" (değişmedi) |

SQL: `hareketler` satırları değişmedi (`SATIS 45000.00/-13.923200`, `ALIS -80000.00/27.846400`) — paket salt okur. `denetim_kayitlari`: `OKUMA · CiktiBelgesi` 57 kayıt (her görüntüleme mevcut `DenetimServisi.OkumaKaydet` ile izleniyor).

### 8.2 Ölçüm tablosu — `sikisma-olc.js` (kendi sekmem, son yayın)
Her hücre: `H1 · TRY · '.00' · eksi has · K1 gizliGörünen · K3 taşan/kırık · K7 hizasız · K8 jargon · K9 binen · yatay`.

| Rota | 1280 açık BASİT | 1280 açık PRO | 1280 koyu BASİT | 1280 koyu PRO | 375 açık BASİT | 375 açık PRO | 375 koyu BASİT | 375 koyu PRO |
|---|---|---|---|---|---|---|---|---|
| SATIS_FISI (satış) | 1·0·0·0·0·0/0·0·0·0·0 | aynı | aynı | aynı | aynı | aynı | aynı | aynı |
| SATIS_FISI (alış) | 1·0·0·0·0·0/0·0·0·0·0 | aynı | aynı | aynı | aynı | aynı | aynı | aynı |
| GIDER_PUSULASI belgesi | 1·0·0·0·0·0/0·0·0·0·0 | aynı | aynı | aynı | aynı | aynı | aynı | aynı |

K2 (en sağ kart / ana alan): 1280 → %90, 375 → %97 (dar sütun yok). K6 seçici: bu ekranlarda seçici yok (0/0).

Ara ölçümler (düzeltmeden önce, kayda geçsin): 1280'de açıklama başlık satırına konunca `h1 "SATIŞ FİŞİ"` kırıldı → açıklama başlık satırının dışına alındı; 375'te `h1 "SATIŞ FİŞİ"`, `th "Has gram"` (fiş) ve `h1 "GİDER PUSULASI"`, `th "Has gram"`, `Yazıyla ⟂ tutar` (pusula) bulundu → § 2.3 / § 2.5 düzeltmeleri; son ölçüm hepsinde 0. Pusulada `.kir-uzun` sonrası kalan K9=1'in satır kutuları (getClientRects) kesişmiyordu (gerçek binme yok); `<br />` ile ölçüm de 0'a indi.

### 8.3 Kâğıt (print) ve ?yazdir=1
Headless Chrome + CDP `Emulation.setEmulatedMedia(print)`, 794×1123 (A4): üç belgede görünür H1 = 1, TRY 0, `.00` 0, eksi has 0, kabuk (üst bar/menü/şerit) görünür = 0. Kâğıt satırları: `Satış 22 Ayar Burma Bilezik 13,9232 has ₺45.000,00` · `Toplam tutar ₺45.000,00` · `Toplam has gram 13,9232 has`.
`?yazdir=1` (S2-5b): `window.print` sayacı ile ölçüldü → sorgulu açılışta **1** çağrı, ardından sorgusuz açılışta ek çağrı **0**.

### 8.4 Ekran görüntüleri (`dokumanlar/09-uygulama-kayitlari/ekran/Q9-4/`)
`ekran-satis-1280-acik-basit.png`, `ekran-satis-375-koyu-pro.png`, `ekran-alis-1280-koyu-pro.png`, `ekran-alis-375-acik-basit.png`, `ekran-pusula-375-acik-basit.png`, `ekran-satis-baski.png`, `ekran-alis-baski.png`, `ekran-pusula-baski.png`.
(Tarayıcı bölmesi görüntülenmediği için `computer screenshot` zaman aşımına düştü; görüntüler aynı vekil üzerinden headless Chrome + CDP ile alındı — betik `scratchpad/ekran.mjs`. Ölçüm tablosu kendi bölme sekmemde koşuldu.)

### 8.5 Temizlik
API (5174), vekil (5173), ölçüm betiği sunucusu (5187), headless Chrome (9337) durduruldu; `lsof` dördünde de boş. Test kiracısı `01M3998KD1H3PMX7RQWKDEE16Y`: `tenant_id` kolonu olan 70 tablodan + `tenants` + kendi davet satırı silindi; kalan satır sorgusu 0 tablo. **tenants: başlangıç 11 → son 11.** `QA-TEST-` kiracılarına dokunulmadı.

## 9. denetim.sh
Son koşum: `DENETİM TEMİZ.` (288 ✓, 0 ✗). Ara koşumda şerit elle çizildiğinde `P-37 SAYFA_BASI_DISI_DUZEN elle=1` ve `P-37c SAYFA_BASI_KAPSAM acik=1` kırmızıydı → SayfaBasi parametresine geçildi, ikisi yeşil.

## 10. DoD sözleşmesi
| Varlık | Düzenleme yolu | Not |
|---|---|---|
| Çıktı belgesi (`OrtakBelge`) | yok — türetilmiş çıktı | Mevcut `DegismezIpucu` (TuretilmisGerekce + KaynaktaDuzelt) yerinde; kayıt kaynağında düzeltilir (ters hareket / storno 48-A). |
| Belge no | yok | Kayıt kimliği; ekranda ne olduğu yazıyor. Uydurma sıra numarası üretilmedi. |
| Yeni varlık / uç / migration | yok | Paket salt okur, deftere yazmaz (HareketFabrikasi'na dokunulmadı). |

Düğmeler: yeni düğme yok; `cikti-geri`, `cikti-yazdir` aynı id ve davranışla şeritte (bUnit ile doğrulandı).

## 11. Değişen dosyalar (bu paket)
Not: depo tek "first commit" üzerinde; bu dosyaların çoğu `git status`'ta dizin düzeyinde `??` görünüyor (`?? dotnet/Kuyumcu.Api/Modules/Cikti/`, `?? dotnet/Kuyumcu.Web/Bilesenler/Yazdirma/`, `?? dotnet/Kuyumcu.Web/Pages/Cikti.razor`, `?? dotnet/Kuyumcu.Web/Pages/PusulaBelgesi.razor`, `?? dotnet/Kuyumcu.Web/Bilesenler/SayfaBasi.razor`, ` M dotnet/Kuyumcu.Web/Layout/UstBar.razor`, `?? dotnet/Kuyumcu.Core/I18n/Sozlukler.CiktiBelge.cs`); paket dosyaları tek tek:
- `dotnet/Kuyumcu.Contracts/Cikti/CiktiSozlesmeleri.cs`
- `dotnet/Kuyumcu.Api/Modules/Cikti/CiktiModulu.cs`
- `dotnet/Kuyumcu.Web/Bilesenler/Yazdirma/A4Belge.razor`
- `dotnet/Kuyumcu.Web/Pages/Cikti.razor`
- `dotnet/Kuyumcu.Web/Bilesenler/SayfaBasi.razor`
- `dotnet/Kuyumcu.Web/Layout/UstBar.razor`
- `dotnet/Kuyumcu.Web/Pages/PusulaBelgesi.razor`
- `dotnet/Kuyumcu.Core/I18n/Sozlukler.CiktiBelge.cs` (yeni)
- `dotnet/Kuyumcu.Tests/Q94CiktiBelgeTestleri.cs` (yeni)
- `dotnet/Kuyumcu.Tests/RotaBaslikTestleri.cs` (tek seçici)
- `dokumanlar/09-uygulama-kayitlari/ekran/Q9-4/*.png` (8 kanıt görüntüsü), bu rapor

Dokunulan sayfalar (TEK DOKUNUŞ kaydı): `Pages/Cikti.razor`, `Pages/PusulaBelgesi.razor`.

## 12. Koşul karşılama
| Koşul | Durum | Kanıt |
|---|---|---|
| Tüm tutarlar ₺ biçiminde (`₺45.000,00`) | ✓ | bUnit + canlı satır metni + print |
| Has gram işaretsiz, virgül 4 hane, kalem = toplam | ✓ | `13,9232 has` her iki yerde; M94-2/3/7 |
| Tek H1 (belge başlığı), tekrar eden H2 yok | ✓ | ölçüm tablosu H1=1 (24 ölçüm), bUnit, M94-8a/8b/9 |
| Belge no okunur numara ya da gerekçe | ✓ (gerekçe) | okunur numara veri modelinde yok; ekranda açıklama, M94-10 |
| Kâğıt (print) çıktı da aynı | ✓ | § 8.3 |
| S2-5b ?yazdir=1 bozulmaz | ✓ | S25b testleri yeşil; canlı print sayacı 1/0 |
| SATIS_FISI, ALIS, GIDER_PUSULASI: TRY/.00 0, eksi has 0, H1 1 | ✓ | § 8.2 tablo, § 8.3 |
| app.css'e ve eşzamanlı şerit dosyalarına dokunulmadı; hub'a dokunulmadı | ✓ | § 11 |
| Test + mutasyon | ✓ | 33 yeni koşum, 19/19 mutasyon kırmızı |
| Gerçek tarayıcı 1280/375 × BASİT/PRO × açık/koyu | ✓ | § 8.2 + § 8.4 |
| Paket yeni kırmızı üretmedi | ✓ | tam süit 5146/5146 (başlangıç 5113/5113) |

## 13. Açık kalanlar
- **Menü grup başlıkları H2** (QA-9 düşük bulgusu, profesyonel modda sayfanın H1'inden önce 6 H2): `Layout/KenarCubugu.razor` kabuk dosyası; bu paketin kabul ölçütü H1 sayısıydı, dokunulmadı. Sahibi: kabuk / menü şeridi.
- **Pusula belgesi başlık sırası**: şerit başlığı pusula no (`H2 GP-2026-000001`) belgenin H1'inden önce geliyor (tekrar değil). İstenirse aynı `BaslikOgesiYok` parametresiyle kapatılabilir. Sahibi: pusula ekranı.
- **Pusula belgesinde dil**: tutarlar `Bicim.BelgeParasi(…, Dil.Locale)` — uygulama dili İngilizce seçilirse `24,045.00 ₺` basılır (Türkçe'de doğru). P-36b'nin bilinçli kararı; bu pakette değiştirilmedi.
- Aynı QA-9 rotasındaki diğer bulgular (mobil üst bar yüksekliği, mod düğmesi açılış titremesi, hidden menü satırları) bu paketin kapsamında değil; K1 ölçümü bugün 0 çıktı (başka şerit düzeltmiş).
- Tam süitte başkasına ait kırmızı: yok.

## 14. Kurucuya / mali müşavire not
- Müşteri fişinde okunur, **sıralı** bir fiş numarası yok; ekranda gösterilen numara sistem kaydının kimliği. Sıralı belge numaralandırması (VUK belge düzeni, 229 vd.) mevzuat konusudur; bu pakette **hiçbir numaralandırma kuralı yazılmadı**. İstenirse ayrı paket olarak birincil kaynak araştırmasıyla ele alınmalı — **mali müşavir teyidi gerekir**.
- Bu pakette mevzuat hükmü içeren bir değişiklik yapılmadı (yalnız sunum/biçim).
