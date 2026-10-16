# PAKET S3-2 — MÜŞTERİLER: LİSTE + ÇEKMECE · Uygulama raporu

- Tarih: 2026-09-24 (00:05–01:40, yerel saat)
- Rol: Taşkın (backend) + Ertuğ (frontend) · Dalga S3A (günlük ekranlar A)
- Durum: **BITTI** (paket kapısının yedi maddesi aşağıda kanıtlı; tam süitte kalan tek kırmızı başka şeridin ürünü, sahibiyle birlikte "Açık kalanlar"da)
- Kaynak: `PLAN-SADELESTIRME-PUSULA-2026-09-23.md` § 3, § 5, § 5B, § 6 S-3 · taslak `dokumanlar/02-mimari/sadelestirme/tuval/project/Musteriler.dc.html` · canvas notu n3

## 1. Hedef ve mevcut durum doğrulaması

| İddia (görev metni / QA tur 8) | Koddaki durum (ölçüldü) | Sonuç |
|---|---|---|
| /cariler satırında 8–9 eylem var | `Cariler.razor` satır eylem hücresinde: Pasife al/Aktife al, Detay, Düzenle, Düzeltme, Tahsilat, Veresiye, Döviz tahsilat, Döviz veresiye (SAHİP'te 8) | Doğru |
| Satır 305–329 px | Canlıda ölçülmedi (eski sürüm çalışmıyor). 8 düğme alt alta sarıyordu, bu yüksekliği açıklıyor | Hedefe göre çalışıldı |
| Düzenleme `Cekmece.razor`'da | `<Cekmece Varlik="cari">` vardı (`#cekmece-cari-kaydet`) | Doğru, korundu |
| Pasife alma satırda (P-38) | `c-pasife-al-{id}` satırdaydı | "⋯" penceresine taşındı, silinmedi |
| Basit modda form ≤ 8 alan | Yeni formda 17, düzenlemede 18 alan vardı; ekran moda bakmıyordu (`ArayuzModuServisi` enjekte değildi) | Uygulandı |

Mevzuat: bu paket hiçbir vergi/MASAK/KDV/tevsik hükmüne dokunmaz. Sunucu kodu, eşik ve yasal kapı değişmedi. PEP beyanı basit modda **gizlenmez**: beyan işaretliyse alan açık kalır (§ 3/6 "basit mod yasal hiçbir şeyi saklamaz"). WebSearch gerekmedi.

## 2. Yapılanlar

### 2.1 Satırda 3 eylem ve "⋯" (iki modda da)
- Görünen eylemler: **Tahsilat** (pasif satırda yerine **Aktife al**) · **Düzenle** · **⋯**. Hepsi `#c-eylem-{id}` içinde.
- Öteki eylemlerin hepsi `#c-menu-{id}` penceresinde: Detay, Veresiye, Düzeltme fişi (rol kapısı aynen), Döviz tahsilat, Döviz veresiye (rol kapısı aynen), Pasife al (onay modalı aynen), Kapat. Pasif satırda Tahsilat da buraya geçer. **Özellik silinmedi.**
- Pencere yerli HTML `popover="auto"` ile yapıldı. **JS yok, CSP istisnası yok**, `index.html` betik sayısı değişmedi. İçerik hep DOM'da durur, tarayıcı kapalı tutar. Bu yüzden bUnit mevcut id'leri bulmaya devam eder. Üst katmanda açılır, `KartListe`'nin `overflow`'u pencereyi kırpmaz (satır içi mutlak konumlu menü kırpılırdı, bu seçeneği bu yüzden reddettim).
- Menüdeki her düğmede `popovertarget` + `popovertargetaction="hide"` var: tek tıkla pencere kapanır ve kendi isteği gider. Canlıda ölçüldü: pencere kapandı, Detay açıldı.
- "Peşin Müşteri" kilit rozeti (`#c-sistem-carisi-{id}`) eylem hücresinden ad hücresine taşındı. Id aynı kaldı.
- Döviz düğmeleri kilitliyse sebep menüde yazılı olarak da görünür (`#c-doviz-kilit-{id}`). Dokunmatikte `title` görünmediği için eklendi (UX-S30).
- Satırlardaki 3 satır içi `style="gap:.5rem"` kaldırıldı, yerine `.cari-bakiye` sınıfı geldi. Dosyada satır içi style 17'den 14'e indi.

### 2.2 Basit mod: sütunlar
- Tür ve Durum ayrı sütun olmaktan çıktı. Ad hücresinde rozet olarak durur: Müşteri dışındaki türler ve "Pasif". Profesyonel modda tablo **birebir bugünkü hâliyle** kalır.
- Döviz sütunu basit modda yalnız listede döviz bakiyesi olan bir hesap varsa çizilir.

### 2.3 Basit mod: form ≤ 8 alan ve "Daha fazla ▾"
- Yeni müşteri formu: **Ad, Telefon, Not** görünür (taslak n3: "form 3 alan + Daha fazla"). `#cari-daha-fazla` düğmesinin metni "Daha fazla (adres, kimlik, e-posta, limit) ▾" olur ve düğme `aria-expanded` taşır. Tıklanınca kalan 14 alan açılır.
- Düzenleme çekmecesi: **Ad, Telefon, Not** ve PEP beyanı işaretliyse PEP alanları görünür. `#duz-daha-fazla` gerisini açar.
- **Gizlemek silmek değildir.** Düzenlemede katlı alanlar sunucudan yüklenen değeri tutar ve PATCH bu değeri **aynen** geri yazar. Birim testi (S32_11) ve canlı SQL bunu kanıtlıyor. Oluşturmada katlı alan boş kalır, yani `null` gider (bugünkü davranış).
- Katlı bir alana hata düşerse grup **kendiliğinden açılır** ve istek atılmaz (S32_12). Bu hata istemci doğrulamasından da gelebilir (ör. eski veride 3 haneli vergi no), sunucunun 400 yanıtından da. Sessiz etkisizlik yok (DoD EK/3).
- Profesyonel modda "Daha fazla" düğmesi çizilmez, DOM bugünküyle aynıdır.
- Mod yetki değildir: `Modu` yalnız çizim kararı verir, sunucu kapılarına dokunulmadı.

### 2.4 Sözlük
Yeni dosya `Kuyumcu.Core/I18n/Sozlukler.Musteriler.cs` (`MusteriListeSozlugu`, TR+EN, konumsal record, eksik anahtar derlemede hata verir). Ortak hub `Sozlukler.cs`'ye dokunulmadı. TR metinlerinde "cari" ya da "storno" jargonu yok, bu S32_14 ile sınanıyor.

### 2.5 CSS
`app.css` sonuna tek bölüm eklendi ("PAKET S3-2"). Sabit renk yok, yalnız belirteç kullanıldı: `.cari-bakiye`, `.cari-satir > td` (dikey boşluk `--bosluk-2`), `.satir-eylem`, `.satir-menu-ac`, `.satir-menu`, `.satir-menu-baslik`, `.satir-menu-ogeler`, `.form-daha-fazla`. İlk sürümde `::backdrop` için `--golge-katman` kullanmıştım. Bu belirteç bir gölge değeri, renk değil; kural geçersiz olduğundan kaldırıldı.

## 3. Sapmalar ve gerekçeleri
1. **"⋯" yalnız simge.** Plan § 4 "yalnız-ikon düğme 0" diyor, taslak (tek doğruluk kaynağı) ise satırda yalnız "⋯" gösteriyor. Taslağa uydum. Düğmede `aria-label="{ad} — Diğer işlemler"` ve `title` var. Orkestratör plan ölçütünü öne alırsa tek satırlık bir değişiklik yeter (sözlükte `DigerIslemler` hazır).
2. **Tür alanı basit modda katlı.** Taslakta yalnız Ad, Telefon, Not var, Tür "Daha fazla"nın içinde. Toptancı açmak bir tık daha istiyor. Taslağa uyuldu.
3. **Telefon zorunlu yapılmadı.** Taslak "Telefon *" gösteriyor ama sunucu telefonu zorunlu tutmuyor. İstemcide zorunlu yapmak kural değişikliği olurdu (varsayım yasağı), yapılmadı.
4. **Sayfa başlığı "Cariler" kaldı.** Plan § 5B'deki "Cari → Müşteri" sözlük göçü ortak hubta (`Sozlukler.cs`, `Nav.Cariler` / `Cari.Baslik`) yapılmalı ve bu dosya bu şeride yasak. Menü "Cariler" derken ekranda "Müşteriler" yazmak UX-S31'i (tek kavram tek ad) bozardı. Göç S-1b / hub sahibine bırakıldı.
5. **Yeni kayıt formu ModalPencere'de kaldı.** Taşımak `#cari-kaydet` id'li testlerin kalıbını bozardı. Kural "ModalPencere düzenleme kullanımı artmaz" ve artmadı (sayaç 52/55). Düzenleme zaten çekmecede.
6. **Taslaktaki "Hepsi / Borçlu / Toptancı" süzgeç çipleri ve "184 müşteri · 5 gösteriliyor" sayacı eklenmedi.** "Borçlu" süzgeci sunucuda yok, yani yeni bir uç gerekirdi (kapsam dondurma). Tür süzgeci aynen duruyor.

## 4. DoD sözleşmesi (varlık → düzenleme yolu)
Bu paket **yeni varlık ya da kayıt türü açmadı**, migration yok. Mevcut Cari varlığının bütün yolları korundu, hepsi ekranda bulunabilir:

| Varlık / işlem | Uç | Ekran yolu |
|---|---|---|
| Cari düzenle | PATCH /cari/{id} | satırda **Düzenle** → çekmece (`#cekmece-cari-kaydet`) |
| Cari pasife al | DELETE /cari/{id} (yumuşak) | **⋯ → Pasife al** → onay modalı (`#c-pasif-onayla`) |
| Cari aktife al | PATCH /cari/{id} {aktif:true} | pasif satırda **görünen ilk eylem** (`#c-aktife-al-{id}`) |
| Telefon (tek alan) | PATCH /cari/{id} | satır içi (`#si-cari-telefon-{id}`) |
| Tahsilat / Veresiye / Döviz | POST /cari/{id}/… | satırda Tahsilat · ⋯ menüsü |
| Düzeltme fişi (finansal) | POST /cari/{id}/duzeltme | ⋯ menüsü (SAHİP/MÜDÜR/MUHASEBE) — hard DELETE yok |
| Peşin Müşteri (sistem kartı) | — | salt okunur: kilit rozeti + çekmecede DegismezIpucu (değişmedi) |

## 5. Derleme
`dotnet build Kuyumcu.sln --nologo` → **0 Uyarı, 0 Hata** (son temiz derleme mutasyonlardan sonra alındı, `pS3-2-b7.log`).
Not: bir ara derleme `Kur.razor`'da `'M' adı geçerli bağlamda yok` hatasıyla kırmızıydı (9 hata). Bu S3-B şeridinin yarım işiydi. O şerit düzeltince derleme yeşile döndü, o dosyaya dokunmadım.

## 6. Testler

| Koşum | Toplam | Geçti | Başarısız |
|---|---|---|---|
| Tam süit ÖNCE (paket başlamadan, 00:07) | 4561 | 4558 | 3 |
| Cariler'i çizen mevcut 9 sınıf (değişiklikten sonra) | 145 | 145 | 0 |
| Yeni `S32MusterilerEkranTestleri` | 15 | 15 | 0 |
| P10 + Sipariş + S32 + CariEkran (düzeltmeden sonra) | 99 | 99 | 0 |
| Tam süit SON (01:3x) | 4654 | 4653 | 1 |

Önceki tam süitin 3 kırmızısı: `S34RaporBugunTestleri.S34_05…`, `SozlesmeKapsamTestleri.Kilitsiz_tip_listesi…`, `SozlesmeKapsamTestleri.Kapsam_orani…`. Hepsi başka şeritlerin ürünü; son koşumda üçü de kapanmış.

Ara tam koşumda (01:05) 7 kırmızı vardı. Bunlardan **2'si bu paketindi**: `P10CariNotEkranTestleri` sınıfı `ArayuzModuServisi` kaydını içermiyordu. Başta yaptığım grep bu sınıfı kaçırmıştı, çünkü sınıf `P10EkranTestleri.cs` dosyasının içinde. Kayıt eklendi, iki test yeşile döndü. Diğer 5 kırmızı başka şeritlerindi: `S21FisYapiTestleri` (S-2), `S34_09` (S3-B), `MigrationHazirlik` ve `YayinAcilis` (test DB'leri o anda yoktu). `SiparisEkranTestleri.TahminiTutarKursuzIseIstekGitmez` tek başına koşunca yeşildi. Son tam koşumda beşi de yok.

**Yeni kırmızı farkı (kapı maddesi 7):** son koşumdaki tek kırmızı `UX4cEkranTestleri.M10_Ayar_listesi_UC_EKRANDA_TEK_KAYNAKTAN_beslenir`. Test `Emanet.razor`'u çiziyor ve "K995" metnini bulamıyor. Emanet.razor S3-B şeridinin dosyası, bu paket ona dokunmadı. **Bu paketin ürettiği kırmızı: 0.**

Yeni testler (dosya `dotnet/Kuyumcu.Tests/S32MusterilerEkranTestleri.cs`): S32_01 (Theory, PRO+BASIT), S32_02 … S32_14. Konuları: satırda 3 eylem ve öteki düğmelerin menüde olması; eski id'lerin menüde durması; popover ve hide sözleşmesi; sistem kartı; menüden gerçek istek (GET ekstre, DELETE); görünen Tahsilat; basit modda sütunlar; döviz sütunu koşulu; yeni form 3 alan ve "Daha fazla"; profesyonel modda katlama olmaması; **katlı alanların PATCH'te aynen gitmesi**; katlı alanda hata olunca grubun açılması; PEP görünürlüğü; sözlük TR/EN ve jargon.

## 7. Mutasyon kanıtları (11/11 kırmızı, 11/11 `cmp` birebir)
Yöntem: kaynak `scratchpad/mut-yedek/S3-2/` altına kopyalandı. Tek eşleşmeli değişiklik yapıldı (Python `count==1` denetimi). Ardından `dotnet build`, sonra filtreli `dotnet test` koşuldu (S32 + CariEkran + PesinMusteri + P38). Yedek geri kopyalandı ve `filecmp` ile içerik karşılaştırıldı.

⚠ İlk turda bir ölçüm hatası yaptım ve düzelttim: `shutil.copy2` geri yüklenen dosyanın **eski mtime'ını** koruyordu. MSBuild bu yüzden sonraki derlemede kaynağı "güncel" sayıp mutasyonlu DLL'i bırakıyordu (M1-3 turunda M1-2'nin izi göründü). Betiğe `os.utime` eklendi, bütün tur baştan koşuldu. Aşağıdaki tablo o temiz ikinci turun sonucu. Reformattan (bkz. § 8) sonra üçüncü kez koşuldu, sonuç aynı çıktı (`pS3-2-mut-sonuc.json`).

| Kod | Mutasyon | Kırmızıya dönen test | cmp |
|---|---|---|---|
| M1-1 | Detay menü öğesinden `popovertargetaction="hide"` silindi | S32_03_Menu_yerli_popover_her_oge_pencereyi_kapatir | AYNI |
| M1-2 | PATCH'te `vergiNo` yalnız "Daha fazla" açıkken gönderiliyor (sessiz silme) | S32_11_Basit_modda_katli_alanlar_PATCHte_AYNEN_geri_yazilir_silinmez | AYNI |
| M1-3 | Doğrulama hatasında grup kendiliğinden açılmıyor | S32_12_Katli_alanda_hata_varsa_grup_kendiliginden_acilir_istek_atilmaz | AYNI |
| M1-4 | PEP koşulu `DuzTumAlanlar` ile sınırlandı | S32_13_PEP_beyanli_kartta_beyan_basit_modda_da_gorunur | AYNI |
| M1-5 | Pasif satırda "Aktife al" görünmüyor (`@if (false)`) | S32_02_Eski_eylemlerin_hepsi_menude_durur_idler_aynen | AYNI |
| M1-6 | `TurDurumSutunu => true` | S32_07_Basit_modda_tur_ve_durum_sutun_degil_ad_hucresinde_rozet | AYNI |
| M1-7 | `DovizSutunu => true || …` | S32_08_Basit_modda_doviz_sutunu_yalniz_doviz_bakiyesi_varsa_cizilir | AYNI |
| M1-8 | `YeniTumAlanlar => true` | S32_09_Basit_modda_yeni_form_uc_alan_Daha_fazla_ile_hepsi_acilir | AYNI |
| M1-9 | Pasife al pasif satırda da çiziliyor | S32_02_Eski_eylemlerin_hepsi_menude_durur_idler_aynen | AYNI |
| M1-10 | EN `DahaFazla` TR ile aynı yapıldı | S32_14_Sozluk_TR_EN_dolu_ve_farkli_jargon_yok | AYNI |
| M1-11 | "Düzenle" düğmesi `DetayAc`'a bağlandı | 13 test; aralarında CariEkranTestleri.Duzenle_acilinca_TAM_KART_cekilir, Kaydet_PATCH_ile_gonderir_ve_TUM_alanlari_tasir, PesinMusteriEkranTestleri.B3E_06…, S32_11, S32_12, S32_13 | AYNI |

Son mutasyondan sonra temiz derleme: 0 Uyarı, 0 Hata.

## 8. Denetim (`dotnet/denetim.sh`)
- İlk koşumda benim yüzümden bir kırmızı çıktı: `Pages: @onclick'siz ve disabled'sız <button>` 74 > tavan 71. Sayaç satır tabanlı bir grep; çok satırlı düğmelerde `@onclick` alt satırda kaldığı için sayılıyordu. Menü öğelerinin ve "Daha fazla" düğmelerinin `@onclick` özniteliği `<button` satırına taşındı. Cariler'in katkısı 15'ten 7'ye indi, bu paket öncesiyle aynı. Sayaç şimdi **68 (tavan 71) ✓**.
- Son koşumda bu paketin sayaçları: ModalPencere 52/55 ✓ · SATIR_ICI_STYLE 110/143 ✓ · P-13 GEREKCESIZ_KILITLI_ALAN 0 ✓ (Cariler taranan 7 sayfadan biri) · P-13 IC_KOD_SIZINTISI 0 ✓ · P-10/P-16 DTO_FORM_ESLESMEZ 0 ✓ · index.html betik sayısı 8 ✓.
- Denetimde kalan iki ✗ bu paketin değil:
  - `P-15: DUZ_SELECT_LIMIT` (ölçülen 13 / tablo 12). `limit=200` sayımı; Cariler.razor'da 0 geçiş var (öncesinde de 0). Artış Islemler/Urunler/Banka … tarafında.
  - `S2-3: FIS_BILESENI_ISTEK` 2. S-2 şeridinin `Bilesenler/Fis/*` dosyaları, aynı kök neden `S21FisYapiTestleri`'nde de görülüyor.

## 9. Canlı doğrulama (port 5191, gerçek Chrome headless + CDP, gerçek veri)
API: `dotnet run --project Kuyumcu.Api --no-build --no-launch-profile --urls http://localhost:5191` (PID 50278). Kiracı, bu paket için açılan tek kullanımlık bir davetle `POST /auth/kayit` üzerinden açıldı: **201**, `PAKETS3-2-TEST`, `01M3831FSR48RQ2NZYR0ASYMN1`. Jeton ve parola ekrana basılmadı. 4 cari `POST /cari` ile açıldı (4 × **201**). Ölçüm betikleri: `scratchpad/pS3-2-canli.js`, `pS3-2-akis.js`. Ekran görüntüleri: `scratchpad/pS3-2-*.png`.

| Adım | Beklenen | Gerçek |
|---|---|---|
| PRO 1440 px liste | satırda ≤ 3 eylem + ⋯, satır ≤ 64 px | Tahsilat · Düzenle · ⋯ (+ satır içi telefon), **57 px** (dikey boşluk daraltılmadan önce), 8 sütun aynen |
| ⋯ penceresi | açılır, ekran içinde, bütün eylemler | `:popover-open` true, 7 öğe (Detay, Veresiye, Düzeltme Fişi, Döviz Tahsilat, Döviz Veresiye, Pasife Al, Kapat), kutu 544,283 352×335, ekran içinde ✓ |
| Menüden Detay | pencere kapanır, detay açılır | menuAcikKaldi=false, detay açıldı ✓ |
| `PATCH /kullanicilar/ben/arayuz-modu` BASIT | 200 | **200** `{"arayuzModu":"BASIT","etkinArayuzModu":"BASIT"}` |
| BASIT 1440 px | Tür/Durum/Döviz sütunu yok, tür rozeti adda, ≤ 64 px | başlıklar: Ad, Telefon, TL Bakiye, Has Gram, Eylemler; "Mehmet Öz Toptan **Tedarikçi**"; ilk ölçüm 69 px → `.cari-satir > td { padding-block: var(--bosluk-2) }` sonrası **61 px** ✓ |
| BASIT 375 px | yatay taşma 0, pencere ekrana sığar | scrollWidth 375 = clientWidth 375 ✓; satır 61 px; pencere 16,227 343×447 ekran içinde ✓ |
| Çekmece (basit) | ≤ 8 görünür alan, liste görünür | görünen: duz-ad, duz-telefon, duz-notlar (**3**); `#duz-vergino` yok; "Daha fazla (adres, kimlik, e-posta, limit) ▾"; genişlik 480 px, liste arkada görünür ✓ |
| Not değiştir → Kaydet | PATCH 200, bant, katlı alanlar korunur | çekmece kapandı, bant "Kaydedildi." ✓ (SQL aşağıda) |
| Daha fazla | katlı alan açılır, değer dolu | `aria-expanded=true`, `#duz-vergino` = "1234567890" ✓ |
| ⋯ → Pasife al → onayla | onay modalı, DELETE, satır listeden çıkar | onay açıldı, menü kapandı, satır listeden çıktı ✓ |
| Pasifleri göster | pasif satırda ilk eylem Aktife al | ilk düğme `c-aktife-al-01M3831YJ…`, ad hücresi "Ali Can **Pasif**" ✓ |
| Aktife al | PATCH, bant | bant "Kayıt yeniden aktif edildi." ✓ |

SQL kanıtı (`cariler`, kiracı `01M3831F…`). Hasan Kaya basit modda yalnız "Not" değiştirilerek kaydedildi, katlı alanlar sağlam:
```
Ali Can     NULL NULL NULL NULL NULL NULL NULL 1
Hasan Kaya  05324112233  hasan@ornek.test  1234567890  Kapalıçarşı No:5  20000.00  30  S32 canli not  1
```
Kayıttan önce Hasan'ın not alanı "eski müşteri"ydi; vergi no, adres, e-posta, limit ve vade kayıttan sonra aynı. Ali Can pasife alınıp geri alındı: `aktif=1`.
`denetim_kayitlari` (DenetimServisi yolu): `EKLEME Cari 5 · GUNCELLEME Cari 3` (not PATCH + pasife al + aktife al) · `OKUMA Cari 14`.

Ölçüm betiğinde bir yanılgıyı ayıkladım: akış betiği ölçüm betiğinin ilk 32 satırından türetilmişti ve o satırlar ilk satırın "Detay"ına tıklıyordu. Ekran görüntüsündeki açık "Ali Can" detay penceresi bu yüzdendi. Zaman çizelgesiyle doğrulandı (30 × 500 ms yoklamada pencere yok, ağ kaydında ekstre isteği yok). Üründe bir kusur değil.

Temizlik:
- API durduruldu (`kill 50278`). `lsof -iTCP:5191 -sTCP:LISTEN` boş.
- `betikler/kiraci-temizle.sh 01M3831FSR48RQ2NZYR0ASYMN1 --uygula`: 70 tablodan 165 satır ve `tenants`'tan 1 satır silindi. Yeniden sorgu: "Kiracı YOK (bağlantı sınandı)".
- Davet satırı `PS32DAVET…` silindi, kalan 0.
- **tenants: başta 10, sonda 10.** QA-TEST-* ve DEMO kiracılarına dokunulmadı.

## 10. Dokunulan sayfalar (DoD EK 2 — tek dokunuş)
- `Kuyumcu.Web/Pages/Cariler.razor` (bu şeridin alanı)

## 11. Değişen dosyalar
```
 M dotnet/Kuyumcu.Web/Pages/Cariler.razor
 M dotnet/Kuyumcu.Web/wwwroot/css/app.css          (yalnız sona "PAKET S3-2" bölümü eklendi)
?? dotnet/Kuyumcu.Core/I18n/Sozlukler.Musteriler.cs
   dotnet/Kuyumcu.Tests/S32MusterilerEkranTestleri.cs   (yeni; Kuyumcu.Tests/ dizini git'te izlenmiyor)
   dotnet/Kuyumcu.Tests/CariEkranTestleri.cs            (+1 satır: AddScoped<ArayuzModuServisi>)
   dotnet/Kuyumcu.Tests/P10EkranTestleri.cs             (+1 satır, P10CariNotEkranTestleri kurucusu)
```
Id envanteri (paket öncesi kopya ile şimdiki dosya): **silinen id 0**. Eklenen id'ler: c-satir-*, c-eylem-*, c-tahsilat-*, c-duzenle-*, c-menu-ac-*, c-menu-*, c-menu-*-baslik, c-detay-*, c-veresiye-*, c-duzeltme-*, c-doviz-kilit-*, c-menu-kapat-*, cari-daha-fazla, duz-daha-fazla.
Migration: **gerekmedi** (şema değişikliği yok). Commit yapılmadı.

## 12. Koşul karşılama
| Koşul | Durum | Kanıt |
|---|---|---|
| Satırda ≤ 3 eylem + ⋯ | ✓ | S32_01 (iki mod) · canlı 3 + ⋯ |
| Satır ≤ 64 px | ✓ | canlı PRO 57 px, BASIT 61 px (1440 ve 375) |
| Düzenleme Cekmece'de, liste kaybolmaz | ✓ | canlı: çekmece 480 px, liste görünür |
| Basit modda form ≤ 8 alan | ✓ | 3 alan (S32_09, S32_11; canlı) |
| Pasife alma ⋯'ye girer, kaybolmaz | ✓ | S32_02, S32_05 (DELETE) · canlı |
| Özellik silinmez | ✓ | eski eylemlerin hepsi menüde, profesyonel modda tablo aynı, "Daha fazla" bütün alanları açar |
| Mevcut id'ler değişmez | ✓ | silinen id 0 |
| Katlı alan PATCH'te korunur (UX-S5, PATCH sözleşmesi) | ✓ | S32_11 + canlı SQL |
| Sessiz etkisizlik yok / hata katlı alanda görünür | ✓ | S32_12 |
| Her düğme istek atar ya da sebep yazar | ✓ | menü öğeleri gerçek işleve bağlı; döviz kilidi yazılı sebep taşır; denetim sayacı 68/71 |
| Sabit renk / satır içi style yok | ✓ | yeni CSS yalnız belirteç; style 17 → 14 |
| CDN/npm/inline script yok | ✓ | yerli popover, betik sayısı 8 |
| Yetki ≠ mod | ✓ | sunucuya dokunulmadı; rol kapıları (Düzeltme, Döviz veresiye) aynen |
| Sözlük ayrı dosya, TR+EN | ✓ | Sozlukler.Musteriler.cs, S32_14 |
| Başka şeridin dosyasına dokunmama | ✓ | yalnız Cariler.razor, app.css sonu, yeni sözlük dosyası ve testler |
| Paket kapısı 1–7 | ✓ | derleme 0/0 · 15/15 · sayaçlar ✓ · 11/11 mutasyon · canlı · kiracı silindi · yeni kırmızı 0 |

## 13. Açık kalanlar (sahibiyle)
1. `UX4cEkranTestleri.M10_Ayar_listesi_UC_EKRANDA_TEK_KAYNAKTAN_beslenir`: `Emanet.razor` "K995" metnini çizmiyor. **S3-B şeridi** (Emanet.razor).
2. `denetim.sh`: `P-15 DUZ_SELECT_LIMIT` 13/12 (tablo güncel değil, yeni `limit=200` noktası). Cariler'de yok; **S-2 ya da Ürünler'e dokunan şerit**.
3. `denetim.sh`: `S2-3 FIS_BILESENI_ISTEK` = 2 (ve `S21FisYapiTestleri` ara koşumda kırmızıydı). **S-2 şeridi** (Bilesenler/Fis/*).
4. Sözlük göçü "Cari → Müşteri" (sayfa başlığı ve menü): ortak hub `Sozlukler.cs`. **S-1b / hub sahibi.**
5. Taslaktaki "Borçlu" süzgeç çipi ve toplam müşteri sayacı sunucuda uç gerektiriyor (kapsam dondurma); oya sunulabilir.
6. "⋯" düğmesi yalnız simge: plan § 4 ölçütü ile taslak çelişiyor (§ 3 sapma 1). Orkestratör kararı gerekir.

## 14. Kurucuya / mali müşavire not
Mevzuat hükmü yok. Mali müşavir teyidi gerektiren bir nokta yok.
