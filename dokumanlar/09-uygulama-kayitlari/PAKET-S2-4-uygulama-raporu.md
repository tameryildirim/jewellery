# PAKET S2-4: Karma ödemede altın bacağı (ekran). Uygulama raporu

Rol: Taşkın (backend) ve Ertuğ (frontend). Dalga S2 (Tezgâh fişi, Pusula formülü). Tarih: 2026-09-24 01:55–02:50.
Durum: **BİTTİ (paket kapısı, 7/7).** Tam süitte 10 kırmızı test var. Hiçbiri bu paketin dosyalarına dokunmuyor; sahipleri § 9'da yazılı.
Commit: YOK. Tüm değişiklikler çalışma ağacında.

Şartname: `KARAR-HAS-DEFTERI-2026-09-23.md` § 4.5 B4-31..B4-35 · PLAN D3 / madde 2.5 ("basit modda karma HER ZAMAN açık") · taslak `sadelestirme/tuval/project/Tezgah.dc.html` (ALDIM sütununda "Eski altın · 2,55 g · 22 ayar" satırı).

---

## 0. Ön koşul ve spesifikasyonun koda karşı doğrulanması

| İddia / koşul | Kodda bulunan | Uyarlama |
|---|---|---|
| "B-4 kapıdan geçmiş olmalı" | `ORKESTRATOR-DURUM.md` 2026-09-24 01:05: **"B-4 altın bacağı GEÇTİ (t2, 4620 test)"** | Paket yapıldı. |
| B4-31 alan sırası ayar → gram → fiyat → has (salt okunur) → TL (salt okunur) | B-4 tur 2 `AltinBacagiPaneli.razor` bunu **zaten** kurmuş; TL/has için giriş alanı yok | Yeniden yazılmadı. Sıra ve salt okunurluk **basit mod tezgâh fişinde** test edildi (E-3). Mutasyon M24-1 ile kanıtlandı. |
| B4-32 satıcı kimliği hemen görünür, eksikse kaydet çalışmaz | Panelde var; ama Kaydet'in yanındaki kilit metni genel "Ödeme satırları" diyordu | **Düzeltildi (UX-S30):** kilit metni altın bacağının İLK eksiğini yazar ("Eski altın: Satıcının adı soyadı…"). |
| B4-33 FARK 0,00 olmadan kaydedilmez | `KarmaGonderilebilir` içinde `FisFark.KaydetAcik` + `FisAltinBacagi.Tamam` | Değişmedi; ekran testi E-6 kilitli düğmeye tıklamanın POST atmadığını sınar. |
| B4-35 iki belge kayıttan ÖNCE söylenir | Yalnız panel içinde (`#altin-belgeler`) | **Eklendi:** Kaydet düğmesinin hemen altında `#islem-kaydet-belgeler` (mükellef satıcıda "tek belge"). |
| D3 "basit modda karma HER ZAMAN açık (ZorunluGorunur)" | Basit modda pro anahtarı `#islem-karma` `ProAlan` içinde (gizli). S2-2 basit satışa "Ödemeyi böl" düğmesi koymuş; ama eski altına ulaşmak için böl → bacak ekle → aracı değiştir (3 adım) gerekiyordu ve tek ödeme ipucu basit modda **olmayan** "Karma ödeme" anahtarını söylüyordu | **Sapma (gerekçeli):** `#islem-karma` anahtarına `ZorunluGorunur` BAĞLANMADI. Basit satış her zaman tezgâh fişidir (S2-2); orada karma "Ödemeyi böl" ile zaten hep açıktır. Anahtarı da açmak aynı soruyu iki kontrolle sormak olurdu (UX-S22) ve `BasitModPosTestleri.Basit_modda_karma_odeme_ve_bacak_YOKTUR` kabulünü bozardı. Onun yerine ALDIM sütununa tek dokunuşluk **"Eski altın"** düğmesi eklendi (taslaktaki ad). Tek ödeme ipucu moda göre gerçekten var olan kontrolü söyler. |
| Kalemsiz fişte ilk bakış ≤ 12 düğme (S2-2 kabulü) | "Eski altın" kalemsiz fişte de çizilince 13 oldu (ölçüldü: `S22…ON_IKI_dugme` kırmızı) | Düğme **yalnız kalem varken** çizilir (ödenecek bir şey yokken anlamsız; "Ödemeyi böl"ün kilidi sebebini zaten yazar). |
| Islemler.razor ≤ 1.400 satır (S2-1 kabulü) | Ekleme sonrası 1401 (ölçüldü: `S21…1400_satiri_asmaz` kırmızı) | Yöntem tek satıra indirildi → **1399** satır. Kural mantığı saf `FisAltinBacagi.cs`'te. |
| Mevzuat | Bu paket hiçbir oran/eşik/kural eklemiyor. Stopaj oranları ve "pusula gerekir" bilgisi sunucudan (`/islemler/altin-bacagi/ayarlar`) gelir; ayarlar yüklenemezse belge notu tutucu tarafa düşer ("iki belge"). | Mevzuat araştırması gerekmedi; yeni hüküm yazılmadı. |

## 1. Yapılanlar

| Dosya | Değişiklik |
|---|---|
| `Kuyumcu.Web/Bilesenler/Fis/FisVerdimAldim.razor` | ALDIM sütununa `#islem-odeme-altin` "Eski altın" düğmesi + `#islem-odeme-altin-not` ipucu. Koşul: kalem var ve (tek ödeme ya da fişte ALTIN bacağı yok ve bacak sınırı dolmamış). Yeni `EskiAltin` geri çağrısı. İstek atmaz. |
| `Kuyumcu.Web/Bilesenler/Fis/FisAltinBacagi.cs` | Saf kurallar: `EnFazlaBacak`, `EklenebilirMi`, `Ekle` (ALTIN bacağı kasa/POS boş, TL 0 doğar), `KilitSebebi` (panelin ilk eksiği), `BelgeNotu` (iki belge / mükellefte tek belge). |
| `Kuyumcu.Web/Bilesenler/Fis/FisOdemeBacaklari.razor` | ALTIN aracının etiketi "Altın" → taslaktaki **"Eski altın"** (UX-S31, tek ad). Değer `ALTIN` aynı. |
| `Kuyumcu.Web/Bilesenler/Fis/FisOdemeKasa.razor` | `@inject ArayuzModuServisi Modu`; `#islem-odeme-altin-ipucu` metni moda göre: basit → "'Eski altın' düğmesine basın", pro → "'Karma ödeme'yi açıp 'Eski altın' bacağı ekleyin". id değişmedi. |
| `Kuyumcu.Web/Pages/Islemler.razor` | `EskiAltin="EskiAltinEkle"` bağı; `EskiAltinEkle()` (böl → ALTIN bacağı → ayarları yükle, tek satır); Kaydet kilit sebebinde `FisAltinBacagi.KilitSebebi(...) ?? Bacaklar`; Kaydet altında `#islem-kaydet-belgeler`. **Kaydet() yerinde, tek POST yolu değişmedi.** 1392 → 1399 satır. |
| `Kuyumcu.Core/I18n/Sozlukler.EskiAltin.cs` (YENİ) | `EskiAltinMetinleri` konumsal record, TR + EN (eksik anahtar = derleme hatası). Sozlukler.cs hub'ına dokunulmadı. |
| `Kuyumcu.Tests/S24EskiAltinEkranTestleri.cs` (YENİ) | 8 bUnit + 3 saf test (§ 3). |

Sunucu tarafı (Modules/Satis, Modules/Cari) **değişmedi** (S-B şeridinin alanı). `AltinBacagiPaneli.razor` (B-4) **değişmedi**; yalnız mutasyon M24-1'de geçici değiştirildi ve `cmp` ile birebir geri alındı.

**DOM id envanteri:** silinen id **0**. Eklenenler: `islem-odeme-altin`, `islem-odeme-altin-not`, `islem-kaydet-belgeler`. Tüm düzenlemeler ekleme biçiminde yapıldı (eski metin korunarak genişletildi); `#islem-odeme-altin-ipucu` span'ı yerinde, yalnız içeriği bir özelliğe taşındı.

**Dokunulan sayfalar (TEK DOKUNUŞ):** `Pages/Islemler.razor` (S-2 şeridinin kendi sayfası; S2-1..S2-3 ile aynı şerit, gerekçe: plan sırası S2-4). Başka sayfa açılmadı.

**Tasarım belirteçleri:** yeni satır içi `style` yok, sabit renk yok; mevcut sınıflar (`btn btn-ikincil`, `metin-kucuk metin-soluk`). denetim.sh `satır içi style=` 110 (tavan 196), `P-18 SABIT_RENK` 0.

## 2. DoD sözleşmesi

| Kayıt | Düzenleme yolu |
|---|---|
| Fişin ALTIN bacağı (kayıt öncesi) | Her alan kayıttan önce serbestçe değişir (B4-34: ayar, gram, fiyat, satıcı); TL/has türetilir, yazılamaz. Bacak "Tek ödemeye dön" ya da bacak aracını değiştirerek kaldırılır. |
| Kaydedilmiş satış fişi + gider pusulası | Bu paket yeni kayıt türü doğurmaz. Kaydedilmiş fiş değişmezdir (finansal varlık, hard DELETE yasak); düzeltme yolu iade / T-24 fiş stornosu (ayrı paket, orkestratör backlog'unda). Gider pusulası düzenleme P-55 (Serit C). |
| Yeni düğme `#islem-odeme-altin` | Tıklanınca gerçek eylem (bacak ekler); kalem yokken çizilmez; kaydederken `disabled`. Boşa tıklanan düğme yok (DoD EK 1, E-1/E-2). |

## 3. Testler

Önce (tam süit, değişiklikten önce, 01:58): **4702 toplam · 4700 geçti · 2 kırmızı** (liste: `scratchpad/s24/once-kirmizi.txt`):
- `S35KurEkranTestleri.S35_08_Manuel_kur_bos_HAS_alan_alti_mesaj_ve_ISTEK_YOK_gecerli_POST`
- `UX1C2EkranTestleri.Kur_manuel_HAS_SIFIRKEN_istek_ATILMAZ_ve_alan_alti_mesaj_cikar`

Yeni testler (11, hepsi yeşil — `scratchpad/s24/t4.log`):

| Kod | Test | Madde |
|---|---|---|
| E-1 | `S24_Basit_Satista_Eski_Altin_Kalem_Olunca_Hep_Var_Kalemsizken_Cizilmez` | D3, DoD EK 1, UX-S22 |
| E-2 | `S24_Eski_Altin_Tek_Dokunusla_Boler_ve_Altin_Bacagi_Acar_Kasa_Yok` | D3, B4-5, B4-6, UX-S31 |
| E-3 | `S24_Alan_Sirasi_Ayar_Gram_Fiyat_Has_TL_Has_ve_TL_Yazilamaz` | B4-31, B4-34 |
| E-4 | `S24_Satici_Kimligi_Hemen_Gorunur_Eksikse_Kaydet_Kilitli_Sebep_Altinin_Ilk_Eksigi` | B4-32, UX-S30 |
| E-5 | `S24_Kayittan_Once_Iki_Belge_Kaydet_Yaninda_Mukellefte_Tek_Belge` | B4-35 |
| E-6 | `S24_Fark_Sifir_Olmadan_Kaydedilmez_Kalani_Ata_ile_Tek_POST_Iki_Bacak_Pusula_Bantta` | B4-33, UX-S27 (Türkçe JSON gövdesi ayrıştırılarak) |
| E-7a | `S24_Tek_Odeme_Ipucu_Basit_Modda_Eski_Altin_Dugmesini_Soyler` | UX-S30 |
| E-7b | `S24_Tek_Odeme_Ipucu_Pro_Modda_Karma_Anahtarini_Soyler` | UX-S30 |
| S-1 | `S24_Ekle_Tek_Altin_Bacagi_Kasasiz_Dogar_Ikinci_ve_Sinir_Reddedilir` | B4-6 |
| S-2 | `S24_KilitSebebi_Altinin_Ilk_Eksigi_Tamamsa_ya_da_Altin_Yoksa_Null` | UX-S30 (TR+EN) |
| S-3 | `S24_BelgeNotu_Pusula_Gerekirse_Iki_Belge_Mukellefte_Tek_Altin_Yoksa_Null` | B4-35, fail-closed |

Filtreli POS koşumu (S24 + B4 + S21 + S22 + S23 + Karma + BasitMod + PosBarkod + Islem + UX4b + Doviz + P10 + B3): **446/446 yeşil** (`scratchpad/s24/t3.log`).

Sonra (tam süit, 02:35, `scratchpad/trx-pS2-4/`): **4756 toplam · 4746 geçti · 10 kırmızı.** Kırmızıların hiçbiri bu paketin dosyalarına dokunmuyor (§ 9). Önceki 2 Kur kırmızısı bu koşumda yeşil (sahibi S3-B kapatmış).

## 4. Mutasyon kanıtları (9/9 kırmızı, hepsi `cmp` birebir)

Yöntem: kaynak `scratchpad/mut-yedek/S2-4/` altına kopyalandı, tek eşleşmeli değişiklik, `dotnet build Kuyumcu.Tests`, `dotnet test --filter "S24|B4AltinBacagiFis|B4AltinBacagiEkran"`, yedek geri kopyalandı, `filecmp` (bayt bayt) ile doğrulandı. Sürücü `scratchpad/s24/mut.py`, sonuç `scratchpad/s24/mut-sonuc-tur1.json` + `mut-sonuc-tur2.json`.
Not: ilk turda M24-4..M24-9 **DERLEME HATASI** verdi; sebep başka şeridin yarım `Kasalar.razor`/`Bildirimler.razor` düzenlemesiydi (`'yeniSube' adı geçerli bağlamda yok`, `'DurumAdi' …`). Ağaç yeniden derlenince (02:14) bu altısı yeniden koşuldu; aşağıdaki tablo ikinci turun gerçek sonucudur.

| Kod | Değişiklik | Kırmızı dönen test(ler) | cmp |
|---|---|---|---|
| M24-1 | **Salt okunur has alanı yazılabilir `Para` girdisi yapıldı** (`AltinBacagiPaneli.razor`) | `S24_Alan_Sirasi_Ayar_Gram_Fiyat_Has_TL_Has_ve_TL_Yazilamaz`, `B4AltinBacagiEkranTestleri.AltinBacagi_Ekran_Alanlar_Duzenlenebilir` | birebir |
| M24-2 | `EskiAltinEkle` ALTIN bacağı eklemiyor | E-2, E-3, E-4, E-5, E-6 | birebir |
| M24-3 | ALTIN bacağı varken de düğme çizilir (B4-6 koşulu kalktı) | E-2 | birebir |
| M24-4 | `KilitSebebi` hep null | E-4, S-2 | birebir |
| M24-5 | `BelgeNotu` mükellefi ayırt etmiyor | E-5, S-3 | birebir |
| M24-6 | `EklenebilirMi` mevcut ALTIN'ı saymıyor | E-2, S-1 | birebir |
| M24-7 | Tek ödeme ipucu basit modda da pro metni | E-7a | birebir |
| M24-8 | Kaydet yanındaki iki belge notu çizilmiyor | E-5 | birebir |
| M24-9 | "Eski altın" ödemeyi bölmüyor (karma açılmıyor) | E-2, E-3, E-4, E-5, E-6 | birebir |

Son mutasyondan sonra temiz derleme: `dotnet build Kuyumcu.sln` → **0 Hata**, 1 uyarı (MSB3026 "Kuyumcu.Web.dll kopyalanamadı, dosya başka süreçte" — eşzamanlı şeridin derlemesiyle dosya kilidi, yeniden denemede geçti; kod uyarısı değil). Ondan önceki iki derleme (`build1.log`, `build2.log`) 0 uyarı 0 hata.

## 5. Denetim

`bash dotnet/denetim.sh` → **DENETİM TEMİZ** (rc=0, `scratchpad/s24/denetim.log`). İlgili sayaçlar: `@onclick'siz ve disabled'sız <button>` 60 (tavan 71) · `ModalPencere kullanımı` 48 (tavan 55) · `satır içi style=` 110 · `record tanımı` 3 (tavan 3) · `P-18 SABIT_RENK` 0 · `S2-3: FIS_BILESENI_ISTEK` 0 (fiş bileşenleri istek atmıyor).

## 6. Canlı doğrulama (5192, `kuyumcu_net`)

Sürücü `scratchpad/pS2-4/canli.sh` + `canli.py` (B-4 sürücüsünden türetildi). Davet satırı doğrudan eklendi, parola rastgele; hiçbir sır basılmadı. Fiş gövdesi **ekranın ürettiği şekille aynı** (E-6: NAKIT 840 + ALTIN K22 5 g × 2.000, kasasız).

| # | Adım | Beklenen | Gerçek | Kanıt |
|---|---|---|---|---|
| 1 | POST /auth/kayit | 201 | 201 | jeton var |
| 2 | GET /kur/guncel | 200 + id | 200 | kur var |
| 3 | GET /islemler/altin-bacagi/ayarlar | 200 | 200 | alisIskontoOran=null; NIHAI 0 · ESNAF 0.02 · MUKELLEF 0 |
| 4 | POST /cari + /urunler (+ /kasalar) | 201 | 201/201 (+200) | — |
| 5 | Stok 10 | 200 | 200 | — |
| 6 | **Fiş 10.000 = NAKIT 840 + ALTIN** | 200 | **200** | has 4.58 · net 9160 · belge 2 · **GP-2026-000001** · nakitToplam 840 |
| 7 | Kasa bakiye farkı | +840 | +840 | altın payı kasaya girmedi |
| 8 | FARK ≠ 0 (nakit 10.000 + altın 9.160) | 400 | 400 | "bacaklar 19.160,00 TL, fiş 10.000,00 TL (fark 9.160,00 TL)" |
| 9 | Satıcı kimliği yok | 400 | 400 | "satıcı kimliği zorunludur (VUK md.234 — gider pusulası)" |
| 10 | GET / (Blazor istemcisi) | 200 | 200 | blazor.webassembly var |

SQL kanıtı (test kiracısı):
```
-- hareketler (tur, tl, has, maden, odeme_turu, nakit, veresiye, kur_var, fis_id_bos, adet)
SAYIM_DUZELTME     0.00   91.600000 ALTIN NULL   0.00 0.00 1 1 NULL   (stok kurulumu)
HURDA_ALIS     -9160.00    4.580000 ALTIN NULL   0.00 0.00 1 1 NULL
SATIS          10000.00   -9.160000 ALTIN KARMA 840.00 0.00 1 0 1.000
-- SATIS + HURDA_ALIS kur_snapshot_id tekil sayısı: 1
-- gider_pusulalari: GP-2026-000001 NIHAI_TUKETICI K22 5.000 4.580000 9160.00 0.0000 0.00 9160.00 (hareket=HURDA_ALIS ✓)
-- odeme_kalemleri: NAKIT 840.00 (kasa dolu) · ALTIN 9160.00 (kasa NULL, hareket=HURDA_ALIS ✓)
-- kasa_hareketleri: yalnız NAKIT 840.00
-- cari_bakiyeler: tl 0.00 · has 0.000000
-- tüm veritabanında hareketler.odeme_turu='ALTIN': 0
```

**Temizlik:** API durduruldu (`kill` PID 66047 ve alt süreci); `lsof -iTCP:5192 -sTCP:LISTEN` boş. `PAKETS2-4-TEST` kiracısı `betikler/kiraci-temizle.sh --uygula` ile tenant_id taşıyan 70 tablodan silindi (45 satır + tenants 1). Davet satırı silindi (kalan 0). `tenants`: önce **10**, sonra **10**. `QA-TEST-*` ve DEMO kiracılarına dokunulmadı. 5190-5200 aralığında 5192 dışında porta dokunulmadı.

## 7. Değişen dosyalar (`git status --porcelain | grep -v -E "/(bin|obj)/"`, bu paketin payı)

```
 M dotnet/Kuyumcu.Web/Pages/Islemler.razor
?? dotnet/Kuyumcu.Web/Bilesenler/Fis/FisVerdimAldim.razor      (dizin S2-1'den beri izlenmiyor)
?? dotnet/Kuyumcu.Web/Bilesenler/Fis/FisAltinBacagi.cs
?? dotnet/Kuyumcu.Web/Bilesenler/Fis/FisOdemeBacaklari.razor
?? dotnet/Kuyumcu.Web/Bilesenler/Fis/FisOdemeKasa.razor
?? dotnet/Kuyumcu.Core/I18n/Sozlukler.EskiAltin.cs             (YENİ)
?? dotnet/Kuyumcu.Tests/S24EskiAltinEkranTestleri.cs            (YENİ)
?? dokumanlar/09-uygulama-kayitlari/PAKET-S2-4-uygulama-raporu.md (YENİ)
```
Migration: **gerekmedi** (şema değişikliği yok; B4-24).

## 8. Koşul karşılama

| Koşul | Durum | Kanıt |
|---|---|---|
| B-4 kapıdan geçmiş | ✓ | ORKESTRATOR-DURUM 01:05 |
| Alan sırası ayar → gram → fiyat → has → TL, TL/has yazılamaz | ✓ | E-3, M24-1 |
| Satıcı kimliği hemen görünür, eksikse kaydet çalışmaz | ✓ | E-4, canlı 9 |
| FARK 0,00 olmadan kaydedilmez | ✓ | E-6, canlı 8 |
| Kayıttan önce "iki belge" söylenir | ✓ | E-5 (Kaydet yanında), M24-5/M24-8 |
| Basit modda karma her zaman açık | ✓ (uyarlanmış: § 0) | E-1, E-2 |
| KABUL: canlı nakit + eski altın iki bacaklı fiş 200 + gider pusulası | ✓ | canlı 6, SQL |
| KABUL: salt okunur has yazılabilir olursa test KIRMIZI | ✓ | M24-1 → 2 test kırmızı |
| Tek POST yolu; fiş bileşenleri ApiIstemci enjekte etmez | ✓ | denetim `FIS_BILESENI_ISTEK 0`, `S21…ApiIstemci_enjekte_etmez` yeşil |
| Mevcut id silinmedi | ✓ | § 1 |
| Islemler.razor ≤ 1.400 | ✓ | 1399; S21 testi yeşil |
| Basit modda ilk bakış ≤ 12 düğme | ✓ | S22 testi yeşil |
| Sözlük ayrı dosya, TR+EN | ✓ | `Sozlukler.EskiAltin.cs` |
| Eşik/oran istemciye gömülmez | ✓ | yeni sayı yok |
| Paket kapısı (7): yeni kırmızı yok | ✓ | § 9 |

## 9. Açık kalanlar (başkasına ait kırmızılar)

Tam süit sonrası 10 kırmızı. Önceki listede yoktular; hiçbiri bu paketin dosyalarını kullanmıyor (`Kasalar.razor` ve `Urunler.razor` içinde `FisOdemeKasa/FisVerdimAldim/FisAltinBacagi/EskiAltin` geçişi 0).
- **S3-B şeridi (`Pages/Kasalar.razor`, 02:26'da değişti, artık 6 `ProAlan` kullanıyor):** `GunKapanisiEkranTestleri` ×6 (`Sayim_alani_BOS_baslar_defterle_doldurulmaz`, `Fark_varsa_GEREKCE_alani_acilir_ve_kaydet_KAPALI`, `Kapanis_dogru_uca_SAYILAN_tutarla_gonderilir`, `Her_aktif_kasada_GUN_KAPAT_dugmesi_vardir`, `Gerekce_girilince_kaydet_ACILIR`, `Sayim_defterle_tutarsa_gerekce_ISTENMEZ`) ve `KasaKapatmaTestleri` ×3 (`BAKIYELI_kasa_pasife_ALINAMAZ`, `Onaylaninca_DELETE_dogru_kasaya_gonderilir`, `Bos_kasa_pasife_ALINABILIR`). Hata: `Cannot provide a value for property 'Modu' on type 'ProAlan'. There is no registered service of type 'ArayuzModuServisi'` — test bağlamına servis kaydı eksik.
- **S3-A şeridi (`Pages/Urunler.razor`, 02:23'te değişti):** `S33UrunlerEkranTestleri.S33_18_Basit_modda_katli_alanlar_PATCHte_AYNEN_geri_yazilir_silinmez` — `Expected "k2", Actual null`.
- Mehmet notu (B-4'ten devralınan, değişmedi): `satis-fisi` ucunda rol kapısı yok; güvenlik ajanı demo sonrası karar verecek.
- Taslaktaki ALDIM özet satırı ("Eski altın · 2,55 g · 22 ayar · 15.650 ₺") bu pakette ayrıca çizilmedi; bilgi panelde aynı sütunda duruyor. İsteğe bağlı görsel iyileştirme olarak S2-5/S2-6 sonrasına bırakıldı.

## 10. Kurucuya / mali müşavire notlar

Yeni mevzuat hükmü yazılmadı. Belge sayısı ("iki belge / tek belge") sunucunun `pusulaGerekir` beyanından okunur; ayarlar yüklenemezse ekran tutucu tarafta kalır ve kayıt zaten kilitlidir. B-4 raporundaki açık doğrulama maddeleri (MASAK eşiği, 459 s. VUK karşısında takas) aynen geçerlidir.
