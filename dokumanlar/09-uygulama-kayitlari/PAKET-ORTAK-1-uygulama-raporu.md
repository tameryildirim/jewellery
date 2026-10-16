# PAKET ORTAK-1 — Ortak görünüm artıkları · uygulama raporu (DEVİR TURU 30.09-c — KEŞİF #4 "görünen ≠ giden", fiş DIŞI formlar)

**Tarih:** 2026-09-30 04:45–06:10 · önceki turlar 25.09, 27–28.09, 29.09, 30.09 (gece), 30.09-b · **Rol:** Taşkın (backend) + Ertuğ (frontend) · **Şerit:** ORTAK (t6 · API 5168 / vekil 5169 · CDP 9611–9617)
**Durum:** BİTTİ — paket kapısının 7 maddesi bu turun değişikliği için sağlandı (kanıtlar aşağıda).
Önceki turların raporu bu dosyada "GEÇMİŞ" başlıkları altında aynen duruyor. Bu turun ham çıktıları: `/private/tmp/kuyumcu-serit/ORTAK-1/c30c/`; ekran görüntüleri: `dokumanlar/09-uygulama-kayitlari/ham/ORTAK-1-ekran/guncel-30c/` (60 dosya).

## 0. Başlangıç denetimi
- Ağaçtaki iş yeniden yazılmadı. Önceki (30.09-b) raporun iddiaları yerinde: `/vezne` BASİT türevleri, test kiracısı yok (tur başı `tenants` = **12**, `PAKETORTAK1%` = 0), yarım mutasyon yok (`c30b/mut-yedek` yedekleri `cmp` ile ağaçtakilerle aynıydı).
- Bu turun TEK bulgusu: **[KRİTİK] KEŞİF #4** — `/urunler` Yeni Ürün > İşçilik Tutarı "1.500" > Kaydet → gövdede `iscilikTl:0` (uyarısız). Değişiklik öncesi kopyalar: `c30c/once/` (her dosyanın ilk hâli).

## 1. Teşhis (koda karşı doğrulandı)
`Bilesenler/Form/SayisalAlan.cs` **kapısız** formda: "1.500" (BELİRSİZ) yazılınca iç hata gösterilir ama `DegerChanged` ÇAĞRILMAZ → sayfa eski değeri tutar (ilk değer 0 ya da tuş tuş yazımın ara değeri "1.5" → **1,5**). Sayfanın bir sonraki yeniden çiziminde `OnParametersSet` `Ondalik("1.500") (null) ≠ Deger` görür, kutuya eski değeri yazar ve **iç hatayı siler** → kutu hatasız "0" / "1,5" olur, Kaydet o değeri POST eder. SATIS-3 bunu `Islemler`/`Fis` için `GecersizAlanKapisi` ile kapatmıştı; kapıyı VERMEYEN her form eski davranıştaydı (`SATIS3EkranTestleri.T17_Kapisiz_formda_SayisalAlan_davranisi_DEGISMEZ` bunu sabitliyor — P78/EMANET1 bu sözleşmeye dayanıyor, o yüzden ortak bileşenin varsayılanı DEĞİŞTİRİLMEDİ).

## 2. Çözüm — SATIS-3 kapısının YENİDEN KULLANIMI (kopya yok, eklemeli, geriye uyumlu)
- `Bilesenler/Form/GecersizAlanKapisi.cs` — yalnız **EK** üyeler: `GecersizAlanKapisi(Func<Task> tazele)` (sayfanın `InvokeAsync(StateHasChanged)`'ini kapatılmış bileşende güvenle çağırır), `Sebep(dil, onek)` (ön ekli ilk geçersiz alanı ETİKETİYLE: Satis3Sozlugu.AlanDuzeltin "«{etiket}» kutusundaki sayıyı düzeltin — kutunun altındaki uyarıya bakın." TR+EN mevcut sözlükten), `FormaIsle(form, onek, mesaj)` (Kaydet'in kod içi ikinci katı). Mevcut üyeler DEĞİŞMEDİ.
- `Bilesenler/Form/SayiKapisiSebebi.razor` (YENİ, 20 satır) — düğmenin yanındaki kilit sebebi satırı (`metin-kucuk metin-amber`, `role="status"`, id `<önek>-sayi-kilit`). Kapı AÇIK parametredir (karmaşık tür → sayfa yeniden çizilince bileşen de tazelenir).
- Her form üç satırla bağlandı: (1) sayfa işaretlemesi `<CascadingValue Value="sayiKapisi" IsFixed="true">` ile sarılır (DOM üretmez; mevcut id'ler aynı), (2) Kaydet `disabled`/`KaydetAktif` + kilit sebebi kapıyı okur (UX-S30), (3) kaydetme yöntemi başında `sayiKapisi.OnekleVar(..)` → `return` (Enter ile gönderim / yarış için ikinci kat). Kapı açık olunca `SayisalAlan` geçersiz metinde çağırana `null` bildirir ve kullanıcının yazdığını EZMEZ (SATIS-3 t17 davranışı).
- `Siparisler.razor` kalem gram/adet kutuları ham `<input type="text">` (SayisalAlan değil): metin zaten korunuyor ve `Olustur` P-78 ile reddediyordu; bu turda kutuya `hatali` + `aria-invalid` ve Oluştur'a PASİF + sebep (`#sip-f-sayi-kilit`) eklendi (`KutuHatali`, `KalemSayiSebebi`).

### 2.1 ENVANTER — Fis/Islemler DIŞINDAKİ tüm sayı/para/gram/yüzde girdileri
| Sayfa / bileşen | Girdi (id) | Yazma yolu | Bu turdaki durum |
|---|---|---|---|
| **Urunler** | yeni `u-gercek-milyem, u-karat, u-gram, u-tas, u-iscilik, u-satis-fiyati, u-kons-bedel, u-alis-maliyeti, u-hedef-marj, u-kdv-orani, u-min-stok, u-acilis-stok` · düzenle `d-*` (11) · sayım `s-adet` · yerleşim `yer-adet-{şube}` | POST /urunler · PATCH /urunler/{id} · PATCH stok · POST yerleşim | **KAPI** (u-/d-/s-/yer-adet-) |
| **Cariler** | `cari-iskonto/limit/vade` · `duz-iskonto/limit/vade` · `dz-tl, dz-has` · `th-tutar, th-has` · `vr-tutar, vr-has` · `dv-tutar` · `dt-tutar` (14) | POST /cari · PATCH · düzeltme · tahsilat · veresiye · döviz | **KAPI** (7 form) |
| **Kasalar** | `h-tutar` · `gun-sayilan` | POST hareket · gün kapanışı | **KAPI** |
| **Banka** | `dekont-tutar` | POST dekont | **KAPI** |
| **Fiyatlandirma** | `fl-marj, fl-carpan` · `km-deger` | POST liste · kampanya | **KAPI** |
| **FiyatListesiUrunPaneli** | `flp-toplu-marj/carpan` · `flp-ozel-sabit/marj/carpan` | toplu önizle/uygula · ürüne özel | **KAPI** (panelin kendi kapısı) |
| **Gider** | `gd-tutar` · `gd-v-tutar` | POST gider · virman | **KAPI** |
| **Uyum** | `sb-tutar` | POST şüpheli bildirim | **KAPI** |
| **SiparisDetay** | `sipd-d-kalem-gram/adet-{n}`, `sipd-d-tahmini` | PATCH sipariş | **KAPI** |
| **Siparisler** | ham `sip-f-kalem-gram/adet-{n}` | POST sipariş | **PASİF + sebep** (metin zaten korunuyordu, P-78 reddi vardı) |
| **Bildirimler** | `is-ayar-saat/vade/masak` | PATCH iş ayarları | **KAPI** |
| **UrunAgaci** | `tasi-adet` · `k-sira` | POST taşıma · kategori | **KAPI** |
| **Kur** | `m-has, m-gram, m-usd, m-eur, m-gbp` | POST manuel kur | **KAPI** |
| **Kurulum** | `kur-satir-{n}-tl`, `kur-satir-{n}-has` | prova · uygula | **KAPI** |
| Hurda | `hrd-*` | — | ZO-ISLEM-A kapısı zaten var (DOKUNULMADI) |
| AltinBacagiPaneli, Fis/* | — | — | SATIS-3 kapısı (DOKUNULMADI) |
| **Emanet** | `e-gram, e-tas, c-gram, c-has` | — | EMANET-1 `HamDenetle` — **fail-closed ama farklı kalıp**: POST gitmez, alan altı mesaj VAR; ancak Kaydet tıklanana dek ETKİN ve kutu sonra "0"a sıfırlanır (kutunun kendi yorumu). Tutarlılık için kapıya geçiş → **EMANET-1** (§ 6) |
| UrunEtiketleri | `et-adet` (kopya adedi) | yazma YOK (yerel taslak + yazdır) | dokunulmadı — § 6 |
| VadeTakibi | `vade-ufuk` | yazma YOK (GET süzgeci, kendi `GirdiDegisti` denetimi) | dokunulmadı |
| Ayarlar | — | — | sayısal girdi YOK (Pages/Ayarlar.razor ve bileşenleri tarandı) |
| Sayim | ham `sy-adet` **type="number"** `@bind decimal` | POST sayım | **AÇIK** — § 6 (tarayıcı "1.500"ü 1,5 okur; sahipsiz) |
| Pos | ham `pos-cihaz-oran/valor`, `pos-taksit-adet/oran` type="number" | POST cihaz/taksit | **AÇIK** → ZO-ISLEM-A (§ 6) |
| Faturalar (10), Evraklar, KasaRaporu, Raporlar (2) | Para/Sayi | — | **AÇIK** → ZO-BELGE (dosya alanı; § 6) |
| Vitrin (3) | Para/Sayi | — | **AÇIK** → KATMAN (§ 6) |

## 3. Testler · mutasyon · denetim
- `dotnet build Kuyumcu.sln --nologo` (son, mutasyonlardan sonra temiz): **0 Uyarı · 0 Hata** (`c30c/build-son2.log`).
- **Yeni test: 34** (hepsi `OK1_K4_*`, tuş tuş "1" · "1." · "1.5" · "1.50" · "1.500" + blur; kutu "1.500" KALIR, `aria-invalid=true`, Kaydet PASİF, sebep kutunun etiketiyle, tıklansa bile yazma isteği YOK; düzeltince yazılan değer gider):
  UrunEkranTestleri 12 (yeni ürün BASİT+PRO × iscilik/gram/taş, düzelt→`iscilikTl=1500` BASİT+PRO, "1,5"→1.5 / "abc"→POST yok, düzenle çekmecesi d-iscilik/d-gram/d-satis, sayım) · CariEkranTestleri 5 (tahsilat, veresiye, düzeltme, düzenle çekmecesi limit, yeni cari limit) · S36KasalarTestleri 3 (hareket BASİT+PRO, gün sonu) · BankaEkranTestleri 1 · FiyatlandirmaEkranTestleri 2 · FiyatListesiUrunGorunumuTestleri 2 · P19EkranTestleri 2 (gider, virman) · UX1C3EkranTestleri 1 (şüpheli bildirim) · SiparisEkranTestleri 2 · BildirimEkranTestleri 1 · YerlesimEkranTestleri 1 · UX1C2EkranTestleri 1 (manuel kur) · KurulumEkranTestleri 1.
- Filtreli: `OK1_K4` **34/34** (`c30c/t11.log` + t13); ilgili ekran sınıfları (Cari, Kasa, Bildirim, Yerleşim, UX1C2, Kurulum, ÜrünAğacı, SADE1, S36, EmanetKur, S35Kur, ORTAK1, OrtakGorunum) **339/339** (`t10.log`), Cari+OK1 **73/73** (`t13.log`).
- **Tam süit** (son kod, KUYUMCU_TEST_DB=kuyumcu_test_t6, trx `/private/tmp/kuyumcu-serit/trx-pORTAK-1/ortak1-30c-son.trx`): **7 699 · geçti 7 697 · başarısız 1 · atlanan 1**. Tek kırmızı `Dogrulayici.KATMAN2SinirTestleri.Asenkron_tasimada_ikinci_tiklama_yazilir_URETIM_benzeri` — `#kenar-grup-dugme-rapor` bulunamadı (KenarCubugu, KATMAN; dosyalar bugün 04:36/05:10'da KATMAN tarafından değiştirildi) → **bu paketin yeni kırmızısı 0**. (Ara koşumda — 05:5x — ayrıca ZO-BELGE'nin FaturaModulu değişikliğine bağlı 4 API testi ve P37 menü araması kırmızıydı; son koşumda yeşile döndüler: başka şeritlerin yarım işi.)
- **Mutasyonlar** (`c30c/betik/mut.py`: kaynak `c30c/mut-yedek/<kod>-<dosya>`'ya kopyalanır → TEK eşleşmeli değişiklik (eşleşme ≠ 1 ise koşum geçersiz) → derleme (kilit varsa yeniden dener) → filtreli test → yedek geri → `cmp` → `touch`). Sonuçlar `c30c/mut-sonuc.txt`; **hepsinde derleme 0 hata, geri alma `cmp` BİREBİR**:

| Kod | Değişiklik | Kırmızıya dönen test(ler) |
|---|---|---|
| M-K4-01 | Urunler `UrunKilitSebebi`'nden kapı çıkarılır | 5: `OK1_K4_yeni_urun_…` ×4, `OK1_K4_virgul_…(abc)` |
| M-K4-02 | Urunler `FormaIsle(form,"u-",…)` silinir | 4: `OK1_K4_yeni_urun_…` ×3, `virgul(abc)` |
| M-K4-03 | Urunler `CascadingValue Value="sayiKapisi"` → `Value="0"` (kapı yok) | 9 (düzenle ×3, yeni ×4, abc, sayım) |
| M-K4-04 | Urunler `DuzenleKilitSebebi`'nden kapı çıkarılır | 3: `OK1_K4_duzenle_cekmecesi_…` (d-iscilik/d-gram/d-satis) |
| M-K4-05 | Urunler `SayimKaydet` koruması silinir | `OK1_K4_sayim_…` |
| M-K4-06 | `GecersizAlanKapisi.Sebep` hep null | 24 (tüm sayfalarda) |
| M-K4-07 | Cariler tahsilat `disabled`'dan kapı çıkarılır | ilk koşumda HAYATTA (test açığı: has 0 iken öbür kilit de tutuyordu) → test güçlendirildi (has "5" geçerli) → `OK1_K4_tahsilat_…` KIRMIZI |
| M-K4-08 | Cariler `TahsilatKaydet` koruması silinir | `OK1_K4_tahsilat_…` |
| M-K4-09 | Cariler düzenle `KaydetAktif`'ten kapı çıkarılır | `OK1_K4_duzenle_cekmecesi_kredi_limiti_…` |
| M-K4-10 | Kasalar `h-kaydet` disabled'dan kapı çıkarılır | `OK1_K4_kasa_hareket_…` ×2 (BASİT, PRO) |
| M-K4-11 | Kasalar `GunKapat` koruması silinir | `OK1_K4_gun_sonu_…` |
| M-K4-12 | Banka `DekontKilitSebebi`'nden kapı çıkarılır | `OK1_K4_dekont_…` |
| M-K4-13 | Fiyatlandırma `fl-kaydet` disabled'dan kapı | `OK1_K4_liste_marji_…` |
| M-K4-14 | Panel `TopluKapali`'dan kapı | `OK1_K4_toplu_marj_…` |
| M-K4-15 | Gider `GiderKilidi`'nden kapı | ilk koşumda HAYATTA (sebep testi `Contains("Tutar")` genel kural cümlesiyle de geçiyordu) → test KAPININ cümlesini BİREBİR ister → `OK1_K4_gider_tutari_…` KIRMIZI |
| M-K4-16 | Uyum `BildirimGecerli`'den kapı | HAYATTA — **eşdeğer mutant**: kapı değeri null yapar, `yeniTutar is > 0` zaten false → kilit iki yoldan da tutar. Yerine M-K4-16b |
| M-K4-16b | Uyum `SayiKapisiSebebi` satırı silinir | `OK1_K4_bildirim_tutari_…` |
| M-K4-17 | SiparisDetay `sipd-d-kaydet` disabled'dan kapı | `OK1_K4_detay_tahmini_…` |
| M-K4-18 | Siparisler `KalemSayiSebebi` gram satırı silinir | `OK1_K4_yeni_sipariste_…` |
| M-K4-19 | `SayiKapisiSebebi` yanlış ön ekle arar | 13 (tüm sebep satırları) |
| M-K4-20 | Bildirimler `is-ayar-kaydet` disabled'dan kapı | `OK1_K4_is_ayari_…` |
| M-K4-21 | ÜrünAğacı `TasimaGecerli`'den kapı | HAYATTA — **eşdeğer mutant** (null→0, `tasiAdet > 0` zaten false). Test sebebi BİREBİR isteyecek biçimde güçlendirildi; yerine M-K4-21b |
| M-K4-21b | ÜrünAğacı taşıma `KilitSebebi`'nden kapı | `OK1_K4_tasima_adedi_…` |
| M-K4-22 | Kur `m-kaydet` disabled'dan kapı | `OK1_K4_kur_manuel_has_…` |
| M-K4-23 | Kurulum `ProvaAcik`'tan kapı | `OK1_K4_acilis_tutari_…` |

  Hayatta kalan (eşdeğer olmayan) mutant: **0**.
- `bash denetim.sh` (son): **DENETİM TEMİZ** (`c30c/denetim.log`). Ara ölçümde P-13 GEREKCESIZ_KILITLI_ALAN = 1 çıktı (yeni cari Kaydet'in kilit sebebi satırı düğmeden 5 satır yukarıdaydı, ölçer −4…+14 bakıyor) → sebep satırı düğmenin hemen altına taşındı → 0. Not: `denetim.sh` zsh ile çağrılınca kendini durdurur ("BASH içindir"), bash ile koşuldu.

## 4. Canlı doğrulama — başsız Chrome, gerçek tuşlar (`Input.insertText` harf harf), Release anlık görüntüsü `c30c/yayin`
Kiracı `PAKET-ORTAK-1-TEST Kuyumculuk 30c` (01M3R2G4FH8HFHRXP0WH7Y0YVA): davet 201, kayıt 201, cari 201×2, kasa 200 + açılış hareketi 200, ürün 201. Betik `c30c/canli.mjs` (sonuçlar `c30c/canli-*.json`); her durumda yazımdan sonra **6 sn** beklenir (kur/yeniden çizim alanın üstüne yazmasın), sonra Kaydet'e tıklanır VE kutuda Enter'a basılır; `window.fetch` sarmalanıp POST sayılır.

| Senaryo | Kombinasyon | Kutu | aria-invalid | Alan altı | Kaydet | Sebep | POST | Düzeltince gövde | Ölçer (K3/K7/K8/K9/yatay) · kontrol (geçerli değerle aynı ekran) |
|---|---|---|---|---|---|---|---|---|---|
| /urunler Yeni Ürün > İşçilik | BASİT açık 1440 | 1.500 | true | "Ondalık için virgül kullanın: 1,5 ya da 1500" | PASİF | «İşçilik Tutarı (₺)» kutusundaki sayıyı düzeltin… | **0** | `iscilikTl:1500` | 0/1/1/6/0 · kontrol 0/1/1/6/0 |
| 〃 | BASİT açık/koyu 1280 | 1.500 | true | 〃 | PASİF | 〃 | **0** | 1500 | 0/1/1/6/0 · aynı |
| 〃 | BASİT açık/koyu 375 | 1.500 | true | 〃 | PASİF | 〃 | **0** | 1500 | 0/0/1/6/0 · aynı |
| 〃 | PRO açık 1440 | 1.500 | true | 〃 | PASİF | 〃 | **0** | 1500 | 0/1/0/6/0 · aynı |
| 〃 | PRO açık/koyu 1280 | 1.500 | true | 〃 | PASİF | 〃 | **0** | 1500 | 0/1/0/6/0 · aynı |
| 〃 | PRO açık/koyu 375 | 1.500 | true | 〃 | PASİF | 〃 | **0** | 1500 | 0/0/0/6/0 · aynı |
| /kasalar + Hareket > Tutar | BASİT açık/koyu 1280, 375 · PRO açık 1440, açık/koyu 1280, 375 (9) | 1.500 | true | 〃 | PASİF | «Tutar» kutusundaki sayıyı düzeltin… | **0** (9/9) | `tutar:1500` (9/9) | 1280/1440: K9 3–4 = kontrol · 375: K9 +1 (bkz. aşağı) · K3/K7/K8/yatay 0 |
| /kur Manuel > Altın has fiyatı | BASİT açık 1280, koyu 375 · PRO koyu 1280, açık 375, açık 1440 | 1.500 (375) / önceden dolu kur + ".500" (1280/1440) | true | 〃 | PASİF | «Altın has fiyatı (TL/gram)» kutusundaki sayıyı düzeltin… | **0** (5/5) | `altinHasFiyati:1500` | K3/K7/K8/yatay 0 · K9 6 = kontrol |

**SQL kanıtı** (kiracı silinmeden önce): `urunler` (C30C*): **10 satır, iscilik_tl = 1500 → 10, = 0 → 0, = 1.5 → 0**; `kasa_hareketleri`: 10 satır = 9 × 1500 + 1 açılış 250000, **0 ya da 1,5 → 0**; `kur_snapshots` MANUAL: 1 satır `altin_has_fiyati = 1500`, **1,5 → 0**. Yani geçersiz durumda HİÇBİR satır yazılmadı, düzeltilen değer BİREBİR yazıldı.

**Ölçer (sikisma-olc K1..K12) — gerekçeli sayaçlar (Anayasa md.1: gizlenmedi):** K9 "binen" çiftlerinin TAMAMI "katman (modal/çekmece) içi öğe ⟂ katmanın ARKASINDAKİ sayfa öğesi"dir (ör. `#sayfa-basi-urunler-aciklama ⟂ Gerçek Milyem`, `#kasa-yeni ⟂ #alan-hata-h-tutar`): ölçer opak katmanın altında kalan sayfa metnini de sayıyor; ekran görüntülerinde üst üste binen yazı YOK. Aynı ekran geçerli değerle (kontrol) ölçüldüğünde sayılar aynı; kasa çekmecesinde 375'te +1, alan altı hata satırının (mevcut `SayisalAlan` öğesi, bu turda eklenmedi) arkadaki "+ Yeni Kasa" düğmesiyle kesişmesidir. K7 (1440/1280 `u-parite` "Parite kodu" satırı ±8) ve K8 BASİT "cari" (Yeni Ürün konsinye bölümü) bu turdan ÖNCE de vardı (kontrol ölçümü aynı; dosyada bu turda dokunulan satırlar değil) → § 6.
**Tur (tur.mjs, başsız Chrome, CDP 9614/9615):** değişen 8 rota `/urunler,/cariler,/kasalar,/banka,/fiyatlandirma,/gider,/uyum,/siparisler` × {BASİT açık, PRO koyu} × {1280, 375}: kayıt 261 · sekme 20 · katman 209 · **hata 0 · geçersiz 0** (`c30c/analiz-SON30e.txt`). Bulgular YALNIZ önceden sahipli iki kalem: genel arama SERBEST-YÜZEN 375 (KATMAN) ve K10 "Tüm kategoriler" PRO 1280 (KATMAN/Secici). Yeni kategori YOK; varsayılan (geçerli) durumda kapı DOM üretmediği için ekran değişmedi.
**Ekran görüntüleri (Read ile incelendi):** `E30c-urun-BASIT-aydinlik-1440-1500.png` (kırmızı kutu + alan altı), `E30c-urun-PRO-karanlik-375-1500.png`, `E30c-urun-PRO-aydinlik-1440-kaydet.png` (sebep + PASİF Kaydet), `E30c-urun-BASIT-karanlik-375-kaydet.png`, `E30c-kasa-PRO-aydinlik-375-kaydet.png`, `E30c-kasa-BASIT-karanlik-1280-kaydet.png`, `E30c-kur-PRO-aydinlik-375-kaydet.png`; ayrıca her kombinasyonun `-1500`, `-kaydet`, `-duzeltildi` görüntüleri (60 dosya).

## 5. Kiracı · süreçler
- API 5168 = ağacın Release anlık görüntüsü (iki kez yayınlandı: ilk dalga + Bildirimler/ÜrünAğacı/Kur/Kurulum sonrası); sırlar yalnız süreç ortamına `~/.kuyumcu/*` (600) dosyalarından; değer yazdırılmadı. Vekil 5169 (fikstür chmod 600).
- Kiracı silme: `zsh betikler/kiraci-temizle.sh 01M3R2G4FH8HFHRXP0WH7Y0YVA --uygula` (bu kez **zsh** ile) → 13 tabloda 357 satır + tenants 1 satır; kendi davet satırım (01M3R2G40PS3M80JE2GMZJPQBF) `ROW_COUNT 1` ile silindi. Doğrulama: 70 `tenant_id`'li tabloda kalan **0** satır; `tenants` tur başı **12**, son **12**; `PAKETORTAK1%` kiracı **0**. QA-TEST-* kiracılarına dokunulmadı.
- 5168/5169 durduruldu (`lsof` boş); başsız Chrome süreçleri kapandı; 5199/5200'e dokunulmadı.

## 6. Açık kalanlar (SAHİBİYLE)
1. **ZO-BELGE** — `Faturalar.razor` (10 sayısal girdi), `Evraklar`, `KasaRaporu`, `Raporlar` (2): kapısız → aynı hata sınıfı olası. Kalıp hazır: `<CascadingValue Value="sayiKapisi">` + `SayiKapisiSebebi` + kaydetme koruması (bu rapor § 2).
2. **ZO-ISLEM-A** — `Pos.razor` ham `type="number"` (`pos-cihaz-oran/valor`, `pos-taksit-adet/oran`): tarayıcı "1.500"ü **1,5** okur (Türkçe binlik ayırıcı) → görünen ≠ giden. `Para/Sayi` bileşenine + kapıya geçmeli.
3. **Sahipsiz / orkestratör** — `Sayim.razor` `sy-adet` ham `type="number"` `@bind decimal`: aynı risk ("1.500" → 1,5 adet sayım). Testleri (`SayimEkranTestleri:115`, `UX1C2:701`) `.Change` kullanıyor; `Sayi` bileşenine geçişte `.Input`'a güncellenmeli.
4. **EMANET-1** — `Emanet.razor` `HamDenetle` fail-closed (POST yok, alan altı mesaj var) ama kalıp farklı: Kaydet tıklanana dek ETKİN, sonra kutu "0"a sıfırlanır. Tutarlılık için kapıya geçiş önerilir.
5. **KATMAN** — `Vitrin.razor` (3 sayısal girdi) kapısız. Ayrıca tam süit kırmızısı `KATMAN2SinirTestleri.Asenkron_tasimada_ikinci_tiklama_yazilir_URETIM_benzeri` (`#kenar-grup-dugme-rapor` yok). Önceki turlardan: genel arama SERBEST-YÜZEN 375, K10 Secici kesik "Tüm kategoriler", P-36c ModalPencere Dispose.
6. `UrunEtiketleri` `et-adet` (kopya adedi) kapısız; sunucuya yazmaz (yerel taslak + yazdır) → düşük risk; `VadeTakibi` `vade-ufuk` GET süzgeci kendi denetimiyle → dokunulmadı.
7. `/urunler` Yeni Ürün modalında önceden var olan ölçer bulguları (bu turda oluşmadı, kontrol ölçümü aynı): K8 BASİT "cari" (konsinye sahibi seçici metni), K7 ±8 "Parite kodu" satırı → ürün ekranının sahibi (sahipsiz; orkestratör).
8. `kiraci_davetleri`'nde doğrulayıcı turlarından kalan iki davet: 01M3QXMQTQ7294A55GH5N6A2SM "PAKETORTAK-1-HAKEM2-TEST …", 01M3QXW75XMA6ZHH0EANGTZRPF "PAKETORTAK-1-HAKEM2B-TEST …" → açan doğrulayıcı / orkestratör (uygulayıcının değil; dokunulmadı).
9. Eşzamanlı derlemeler: bu turda iki kez başka şeridin `dotnet build/test`'i `obj/Kuyumcu.Tests.dll`'yi kilitledi (CS2012 / MSB3026) ve bir filtreli koşum eski DLL ile koştu (27 test görünüp 30 olması gerekirken) → `c30c/betik/derle.sh` kilitte 15 sn arayla yeniden dener; mutasyon ve tam süit bununla koşuldu. Orkestratör notu: şerit başına ayrı `obj/bin` ya da sıralı derleme.

## 7. DoD sözleşmesi
Yeni varlık/kayıt türü, uç, PATCH/DELETE, migration YOK (**migration gerekmedi**). Kaydet()/POST yolları, yetki ve yasal kapılar DEĞİŞMEDİ; yalnız istemci tarafında "anlaşılamayan sayı varken gönderme" kapısı eklendi (her düğme ya gerçek istek atar ya PASİF + sebep yazar — DoD EK md.1; boş/geçersiz gönderimde alan altı Türkçe mesaj — md.3). Mevcut id'ler korundu; yeni id'ler `<önek>-sayi-kilit` (u-kaydet-kilit ve Çekmece `-kilit` mevcut öğeleri kullanıldı) ve `vr-kaydet` (veresiye Kaydet'ine id eklendi). Yeni metin YOK: sebep cümlesi mevcut `Satis3Sozlugu.AlanDuzeltin` (TR+EN), alan altı `SayiGirisiSozlugu` (TR+EN). Yeni CSS YOK (mevcut `metin-kucuk metin-amber`); satır içi style eklenmedi.
**Dokunulan sayfalar (bu tur):** Urunler, Cariler, Kasalar, Banka, Fiyatlandirma, Gider, Uyum, SiparisDetay, Siparisler, Bildirimler, UrunAgaci, Kur, Kurulum + bileşen FiyatListesiUrunPaneli. İkinci dokunuş gerekçesi (Gider, Uyum, Siparisler, Bildirimler, UrunAgaci önceki ORTAK-1 turlarında da açılmıştı): orkestratörün KEŞİF #4 talimatı (aynı paketin yeni kritik bulgusu).
**Alan dışı dokunuş (gerekçeli):** orkestratör bu turda formları ADIYLA saydı ("/urunler … /cariler, /kasalar, /banka dekont, /fiyatlandirma, /siparisler, /gider, /emanet, /uyum, /ayarlar"). `Urunler, Cariler, Fiyatlandirma, FiyatListesiUrunPaneli, Kur, Kurulum` hiçbir eşzamanlı şeridin listesinde değil; `Kasalar` (KATMAN) ve `Banka` (ZO-ISLEM-A) için yalnız EKLEMELİ 3 satırlık kalıp uygulandı (Kaydet koşuluna bir terim + sebep satırı + kod içi koruma; başka satır değişmedi). `Emanet` (EMANET-1) — talimat "tutarlı mı bak" dediği için DOKUNULMADI, § 6-4. Ortak bileşen `Bilesenler/Form/GecersizAlanKapisi.cs` SATIS-3'ün; yalnız EK üyeler (orkestratör: "ortak bileşen değişikliği EKLEMELİ ve geriye uyumlu") — `T17_Kapisiz_formda_SayisalAlan_davranisi_DEGISMEZ` ve P78/EMANET1 testleri yeşil (tam süit).

## 8. Koşul karşılama (30.09-c)
| Koşul | Durum | Kanıt |
|---|---|---|
| KRİTİK KEŞİF #4: /urunler İşçilik "1.500" sessizce 0 | ✓ | canlı 9 kombinasyon POST 0, düzeltince `iscilikTl:1500`; SQL 10/10 = 1500; bUnit `OK1_K4_yeni_urun_…` ×4 + düzelt ×2 |
| (1) ENVANTER | ✓ | § 2.1 (Fis/Islemler dışı tüm Para/Gram/Sayi + ham sayısal girdiler) |
| (2) geçersiz/belirsiz girdide alan hatası + Kaydet PASİF + sebep + POST'a eski/0 gitmez; SATIS-3 kapısının YENİDEN kullanımı | ✓ (13 sayfa + panel) · açıklar § 6 (sahibiyle) | § 2; 34 bUnit; canlı /urunler, /kasalar, /kur |
| (3) sıra: /urunler → /cariler, /kasalar, /banka, /fiyatlandirma, /siparisler, /gider, /uyum, /ayarlar; /emanet tutarlılık | ✓ (/ayarlar'da sayısal girdi yok; /emanet § 6-4) | § 2.1 |
| Her form bUnit (tuş tuş "1.500" → POST 0) | ✓ | § 3 (34 test) |
| En az 3 mutasyon | ✓ (25 mutasyon, 27 koşum; eşdeğer olmayan hayatta kalan 0) | § 3 tablo |
| Başsız Chrome canlı kanıt (/urunler iscilik) | ✓ | § 4 (+ kasa, kur) |
| Gerçek tarayıcı kuralı: 1280/375 · BASİT/PRO · açık/koyu, K1..K12, ekran görüntüsü | ✓ (K9 gerekçeli: katman arkası; kontrolle aynı) | § 4 |
| Build 0/0 · paket testleri · mutasyon · denetim · yeni kırmızı 0 · kiracı silindi | ✓ | § 3, § 5 (tam süit 7 697/7 699, tek kırmızı KATMAN) |

## 9. Kurucuya / mali müşavire not
Mevzuat hükmü içeren değişiklik YOK. Hesap, sunucu kuralı ve gövde alanları değişmedi; yalnız ekranda "anlaşılamayan sayı varken gönderme" kapısı eklendi.

---

# GEÇMİŞ — 30.09-b devir turu raporu (aynen)

# PAKET ORTAK-1 — Ortak görünüm artıkları · uygulama raporu (DEVİR TURU 30.09-b — hakem turu 30.09 bulguları)

**Tarih:** 2026-09-30 02:50–03:50 · önceki turlar 25.09, 27–28.09, 29.09, 30.09 (gece) · **Rol:** Taşkın (backend) + Ertuğ (frontend) · **Şerit:** ORTAK (t6 · 5168/5169 · CDP 9609–9617)
**Durum:** BİTTİ — paket kapısının 7 maddesi güncel ağaçta yeniden sağlandı (kanıtlar aşağıda).
Önceki turların raporu bu dosyada "GEÇMİŞ" başlıkları altında aynen duruyor. Bu turun tüm ham çıktıları: `/private/tmp/kuyumcu-serit/ORTAK-1/c30b/`.

## 0. Başlangıç denetimi
- Ağaçtaki iş yeniden yazılmadı; yalnız hakem bulgularına dokunuldu. Değişiklik öncesi yedekler: `c30b/once/*.once` (KarZarar.razor, KarZararEkranTestleri.cs, Vezne.razor, ortak.css, Bicim.cs, ORTAK1SinirTestleri.cs).
- Önceki "SON DOĞRULAMA" iki maddesi bu turda yeniden denetlendi: (a) `/vezne` BASİT "Vezne" türevleri — `Vezne.razor` artık `VzVezneyeGonder/VzVeznelendi/VzToplam*` ile BASİT dalı taşıyor (satır 1070–1076), ekran görüntülerinde BASİT "Kasa kuyruğu / Kuyrukta"; (b) 'PAKETORTAK-1-TEST Dogrulama' kiracısı: `SELECT … FROM tenants` → YOK (tur başı 15 kiracının hiçbiri ORTAK-1 değil).

## 1. Hakem turu 30.09 bulguları — kapatma tablosu

| Bulgu (önem) | Yapılan (dosya) | Kilit test | Mutasyon → kırmızı | Tarayıcı kanıtı (`ham/ORTAK-1-ekran/guncel-30b/`) |
|---|---|---|---|---|
| **[KRİTİK]** KarZarar BASİT has kaybı uyarısı "0,00 gram KAYBETTİNİZ" (kendisiyle çelişki) | Uyarı artık gramı `GramSayi()` → `Bicim.KarHasGram(|KarHas|, BasitMi)` ile yazar: BASİT'te 2 haneye yuvarlanınca 0'a düşen sıfırdan farklı değer 4 haneye kadar ("0,004") | Hakem OK8 ✓ · `KarZararEkranTestleri.ORTAK1_HK30_has_kaybi_uyarisi_kayip_gramini_sifir_yazmaz` (BASİT "0,004", PRO "0,0040"; metin sözlükten `string.Format` ile BİREBİR) | M87, M87b → OK8 + HK30_has_kaybi | Canlı (fetch sarmalayıcı karHas=−0,004, karTl=1500): BASİT 1440/960-koyu/375 "…1.500 ₺ kâr yazdınız ama **0,004 gram** has KAYBETTİNİZ"; PRO 1280-koyu "0,0040 gram" — `E30b-karzarar-*-hasbolum.png` |
| **[KRİTİK]** BASİT sıfırdan farklı has (0,0001 g) "0,00 g" | `Bicim.KarHasGram` (yeni, ek metot): PRO `N4` aynen; BASİT `N2`, ANCAK `Round(n,2)==0 && n≠0` ise `#,##0.00##` ("0,0001", "-0,004"); `Round(n,4)==0` sıfır sayılır. `0,005 → 0,01` (AwayFromZero) korunur | Hakem OK1–OK8 ✓ · `ORTAK1_HK30_kucuk_has_sifirdan_farkliysa_sifir_gibi_gorunmez` ×7 (0,0001 / −0,004 / 0,005 / −1,2549 / 0 BASİT; 0,0001 / −0,004 PRO) | M86, M86b → OK3 + OK8 + HK30 ×3 | Canlı kart değerleri BASİT: `0,0001 g · 0,00 g · -0,004 g · 0,00 g`; PRO: `0,0001 g · 0,0000 g · -0,0040 g · 0,0000 g` (1440/1280/960/375, açık+koyu) |
| [DÜŞÜK] Gram biçimi tarayıcı kültürüne bağlı | Biçim tek merkez `Bicim.KarHasGram` (tr-TR SABİT). Razor'da `ToString("N2"/"N4")` kalmadı (denetim P-36b GIRDI_AILESI_DISI_BICIM ✓ 0 — ara adımda razor içi `ToString("N2", …)` bu sayacı 1 yapmıştı, merkeze taşındı) | `ORTAK1_HK_bacak_…` ve HK30 testleri ekranı **en-US iş parçacığı kültüründe** çizer | M88, M88b (Tr → CurrentCulture) → bacak(PRO) + HK30 PRO ×3 (+M88: OK1/2/3/4/6/7, P35_04, P45_04 dahil 19) | Başsız Chrome `Emulation.setLocaleOverride en-US` (navigator.language = en-US, `(1234.5).toLocaleString()` = "1,234.5"): BASİT `-1.234,57 g`, PRO `-1.234,5678 g`, uyarı "1.234,57 gram" — `E30b-karzarar-{BASIT,PRO}-acik-1280-enUS.png` |
| [DÜŞÜK] Kendini doğrulayan / kaynak-dizgesi testleri | `ORTAK1_HK_bacak_…` beklentisi SABİT tr-TR ("0,66 g", "0,6591 g", "0.66 g" yok). Geometri iddiası tarayıcı ölçümüne bağlandı (§ 2 tur + MUT-B31) | — | M88/M88b ile kırmızı | — |
| [DÜŞÜK] /vezne 960 Toplam rozeti kap kenarında kesik | `Vezne.razor` rozet kabı `class="vz-toplam-rozet"` (id aynı); `ortak.css`: rozet tutarın ALTINA iner (`display:block`), `white-space:normal; max-width:12rem; border-radius: var(--radius-md)` | `OrtakGorunumHakemTestleri.HK16_vezne_toplam_rozeti_tutarin_altina_iner_ve_satir_kirar` | M89, M90, M90b, M92, M93 → HK16 | Ölçüm (vz-olc.js): rozet tutarın altında, taşma 0 (tüm genişlikler); tablo genişliği 960'ta **824 px** (MUT-B31: kural kaldırılınca **942 px**, eylem 118 px daha dışarıda → geri alındı `cmp` birebir). 1280/1440'ta tablo kaba tam sığar, eylem görünür. 960/1024'te tablo (A) kalıbıyla kendi kabında kayar (sayfa kaydırma 0) |
| [DÜŞÜK] denetim P-23 kırmızı | Pakete ait değil — § 6 | — | — | canlı SQL: 2 satır, ikisi de "TEST-KESIF Kuyumculuk" (01M3QG1P6EVK9HM5CVRTCBKWXJ KART 29.09 21:10; 01M3QQEA86CZ0ADP59MXZTBVEX NAKIT 29.09 23:19) |

**Denenen ve GERİ ALINAN (Anayasa md.1):** 960–1365 px aralığında `#vz-liste` eylem sütununu sağa yapışık (sticky) yapmak. Tur ölçümü (`c30b/analiz-SON30c.txt`) yapışık hücrenin altından kayan "Ayşe Yılmazoğlu" hücresiyle **K9 binme, 8 durum** (B/P × açık/koyu × 960/1024) saydı; 768'de de Toplam dahil üç sütunu örtüyordu. Kural kaldırıldı, HK16 kuralın geri gelmemesini iddia eder (M93 → HK16 kırmızı). Yeniden ölçüm `analiz-SON30d.txt`: /vezne K9 **0**.

**Sapma — hakemin OK3 testi düzeltildi (gerekçe):** `ORTAK1SinirTestleri.OK3` özgün iddiası `DoesNotContain("0,00 g|", tümKartlar)` idi. Bu fikstürde hasılat ve maliyet has'ı GERÇEKTEN 0'dır ve hakemin kendi OK1'i sıfırın BASİT'te "0,00 g" basılmasını ister — iki iddia birlikte sağlanamaz (düzeltmeden sonra kâr kartı "0,0001 g" iken test yine hasılat/maliyet sıfır kartlarına takılıyordu: `…|0,00 g|0,00 g|0,0001 g|…`). Yorumdaki niyet korunarak iddia sıralı üçlüye daraltıldı: hasılat "0,00 g", maliyet "0,00 g", kâr "0,0001 g". M86/M86b'de OK3 yine kırmızı → sahte yeşil değil. Özgün hâl: `c30b/once/ORTAK1SinirTestleri.cs.once`.

**Sapma — dosya alanı:** `Kuyumcu.Web/Servisler/Bicim.cs` ORTAK-1'in dosya alanında değil (BICIM-TEK sahipsiz). Hakem "Bicim merkezini kullan" dedi ve denetim P-36b razor içi biçimi yasaklıyor; bu yüzden **yalnız EK** bir metot (`KarHasGram`) eklendi, mevcut hiçbir metot değişmedi. Dosya 25.09'dan beri kimsece değişmemişti (mtime).

## 2. Kabul ölçümü — tur.mjs başsız Chrome (Release anlık görüntüsü `c30b/yayin`, API 5168, vekil 5169)
Tohum: kiracı `PAKET-ORTAK-1-TEST Kuyumculuk 30b` (01M3QT6TE9QQPGSEANRC762AGQ): 12 cari (uzun adlar), 8 veresiye (6–7 hane), 2 kasa (7.681.959,97 ₺ + USD) + açılış hareketi, 1 gider kategorisi + 2 gider, 3 sipariş (S-2026-000001..3), 4 vezne fişi (2'si kuyrukta); sahip adı "Ayşe Yılmazoğlu" (PATCH /kullanicilar/ben 200). HTTP: davet 201, kayıt 201, cari 201×12, veresiye 200×8, kasa+hareket 200×4, gider kategorisi 200×1 + gider 200×2, sipariş 400×3 (kalemsiz ilk deneme) → kalemli 200×3, vezne fiş+kuyruğa gönder 200×6.
- **Tur 30c** (`c30b/tur-SON30c`, 6 rota: /vezne, /raporlar/kar-zarar, /, /kullanicilar, /uyum, /isletme × {BASİT,PRO} × {açık,koyu} × {1440,1280,1024,960,768,375}): kayıt 778 · hata 0 · geçersiz 0 → yapışık sütun K9 8 durum (yukarıda, geri alındı).
- **Tur 30d** (`c30b/tur-SON30d`, düzeltme sonrası, /vezne + /raporlar/kar-zarar, 24 kombinasyon): kayıt 344 · hata 0 · geçersiz 0 → `c30b/analiz-SON30d.txt`.

| Kategori | 30.09 gece SON (30b) | **30.09-b SON (30c/30d)** |
|---|---|---|
| A tablo başlığı / ad hücresi kırık | 0 | **0** |
| B para kartı taşıyor | 0 | **0** |
| C filtre hizasız · D çekmece sayfa-kaydı · E Kim borçlu · F · H | 0 | **0** |
| G BASİT jargon (ölçer K8 ürün metni) | 0 | **0** (/isletme K8 "PAKET-ORTAK" = test kiracısının ADI) |
| K9 binme (/kullanicilar, /vezne) | 0 | **0** (30d) |
| sayfa yatay kaydırma | 0 | **0** (tüm problarda `yatay 0`) |
| KarZarar has 0,0001 / kayıp 0,004 sıfır gibi | (hakem: 2 KRİTİK) | **0** — canlı değerler § 1 |

Kalan bulgular (tümü önceden sahipli/gerekçeli, yeni kategori YOK): genel arama SERBEST-YÜZEN 375/PRO 768 (KATMAN) · K10 "Altınkaynak (varsayılan)" PRO 768 (KATMAN/Secici) · / K4 tren 2.1× BASİT 375 (29.09 gerekçesi) · /vezne "Neden değiştirilemez?" ekran-dışı (akış içi `<details>`, 29.09 § D gerekçesi; koordinatlar tablo daraldığı için değişti, durum kümesi aynı).

**Ekran görüntüleri (hepsi Read ile incelendi)** — `ham/ORTAK-1-ekran/guncel-30b/`: E30b-karzarar-BASIT-acik-1440-kucukhas, E30b-karzarar-BASIT-aydinlik-1440-hasbolum, -BASIT-karanlik-960-hasbolum, -BASIT-aydinlik-375-hasbolum, -PRO-karanlik-1280-hasbolum, E30b-karzarar-{BASIT,PRO}-acik-1280-enUS, E30b-vezne-BASIT-aydinlik-960(-son), -BASIT-karanlik-1024, -PRO-aydinlik-768, -BASIT-karanlik-1280-son, -PRO-karanlik-1024-son, -PRO-aydinlik-375-son, E30b-vezne-MUT-B31-960 (+ tur görüntüleri `c30b/tur-SON30c/foto`, `tur-SON30d/foto`).
Tuval: değişen ekranların (Kâr-zarar, Kasa kuyruğu) tuvali yok (`TUVAL-png`: Ayarlar/Ekranlar/Giris/Musteriler/Panel/Profesyonel/Tezgah); Panel bu turda değişmedi.

## 3. Testler · mutasyon · denetim
- `dotnet build Kuyumcu.sln --nologo` (son): **0 Uyarı · 0 Hata** (`c30b/build8.log`).
- Yeni test koşumları: `KarZararEkranTestleri.ORTAK1_HK30_kucuk_has_…` (7) + `ORTAK1_HK30_has_kaybi_…` (2) + `OrtakGorunumHakemTestleri.HK16` (1) = **10**; güncellenen: `ORTAK1_HK_bacak_…` (×2, sabit tr-TR + en-US kültür), hakem `OK3` (çelişki düzeltmesi).
- Filtreli (KarZarar | OrtakGorunum | ORTAK1 | Vezne | Bicim): **437/437** yeşil (`c30b/test7.log`).
- **Tam süit** (son kod, 03:4x, KUYUMCU_TEST_DB=kuyumcu_test_t6, trx `/private/tmp/kuyumcu-serit/trx-pORTAK-1/ortak1-30b-son.trx`): **7 581 · geçti 7 581 · başarısız 0** → paketin yeni kırmızısı **0**.
- **Mutasyonlar** (`c30b/mut.sh`; yedek `c30b/mut-yedek/`; her biri geri kopyalandı, `cmp` birebir; derleme hatası veren koşum sayılmaz — M87'nin ilk denemesi eşzamanlı derleme yüzünden (apphost/WebAssembly MSB hatası) geçersiz sayılıp yeniden koşuldu; hayatta kalan **0**):

| Kod | Değişiklik | Kırmızıya dönen test(ler) |
|---|---|---|
| M86 | razor BASİT küçük-değer dalı → düz N2 | OK3, OK8, HK30_kucuk(0.0001), HK30_kucuk(−0.004), HK30_has_kaybi(basit) |
| M86b | `Bicim.KarHasGram` küçük-değer dalı → N2 | aynı 5 test |
| M87 | uyarı gramı `ToString(BasitMi?"N2":"N4")` | OK8, HK30_has_kaybi(basit) |
| M87b | uyarı gramı önce 2 haneye yuvarlanır | OK8, HK30_has_kaybi(basit), HK30_has_kaybi(PRO) |
| M88 | razor kültürü → CurrentCulture | 19 test (OK1/2/3/4/6/7, ORTAK1_HK_bacak ×2, HK30 ×8, P35_04, P45_04) |
| M88b | `Bicim.KarHasGram` PRO → CurrentCulture | ORTAK1_HK_bacak(PRO), HK30_kucuk(PRO ×2), HK30_has_kaybi(PRO) |
| M89 | Vezne rozet kabından `vz-toplam-rozet` sınıfı sil | HK16 |
| M90 / M90b | `.vz-toplam-rozet` `display:block` → `inline` | HK16 |
| M91 | (geri alınan) yapışık sütunda `position:sticky` sil | HK16 (o anki sürüm) |
| M92 | rozet `white-space:normal` → `nowrap` | HK16 |
| M93 | yapışık eylem kuralı geri eklenir | HK16 |
| MUT-B31 (tarayıcı) | anlık görüntü ortak.css'te rozet `inline`+`nowrap` | canlı ölçüm: tablo 824 → 942 px, eylem sağ 1141 → 1259; geri alındı `cmp` birebir, tekrar ölçüm 824 |

- `bash denetim.sh` (son): paketin tüm sayaçları ✓ (P-36b GIRDI_AILESI_DISI_BICIM **0**, ModalPencere, satır içi style, hex, emoji…). Tek ✗: **P-23 ODEME_KALEMSIZ_YOL = 2** — canlı DB; iki satır da "TEST-KESIF Kuyumculuk" kiracısının (01M3QFYWC18SPG25CTE5YCAS1H) — bu paketin değil (§ 6).

## 4. Canlı doğrulama · kiracı · süreçler
- API 5168 = ağacın Release anlık görüntüsü (`dotnet publish -c Release -o c30b/yayin`, iki kez: ilk ve Bicim/css son hâli); sırlar yalnız süreç ortamına `~/.kuyumcu/*` (600) dosyalarından; değer yazdırılmadı. Vekil 5169 (fikstür chmod 600). Backend (API) değişikliği yok.
- KarZarar küçük has değerleri gerçek satışla üretilemediği için tarayıcıda `window.fetch` sarmalanıp YALNIZ özet yanıtının `karHas/karTl/hasilatHas` alanları değiştirildi (sarılan istek sayısı ölçümde 1) — ekranın biçim/uyarı yolu gerçek WASM kodudur.
- Kiracı silme: `kiraci-temizle.sh` yanlışlıkla **bash** ile koşuldu (betik zsh; `${=TABLOLAR}` "bad substitution") → yalnız `tenants` satırı silindi, alt tablolar kaldı. Aynı mantık zsh ile elle uygulandı: 19 tabloda **1 053 satır** silindi, kalan **0**; `kiraci_davetleri` artığı 1 satır (ROW_COUNT 1) silindi. Doğrulama: `tenants WHERE id=… OR REPLACE(ad,'-','') LIKE 'PAKETORTAK1%'` = **0**; davet süzgeci = **0**. QA-TEST-* kiracılarına dokunulmadı.
- tenants: tur başı **15**, son **15** (arada başka şeritler kendi kiracılarını açıp kapattı: SATIS-3 t24 ×2 gitti, KATMAN-1b-HAKEM ve SATIS-3 Dogrulama geldi — bunlar bu paketin değil).
- 5168 ve 5169 durduruldu (`lsof` boş); başsız Chrome süreçleri kapandı; 5199/5200'e dokunulmadı.

## 5. Değişen dosyalar (bu tur; `git status --porcelain | grep -v -E "/(bin|obj)/"`)
```
 M dotnet/Kuyumcu.Web/Servisler/Bicim.cs            (EK: KarHasGram — alan dışı, gerekçe § 1)
?? dotnet/Kuyumcu.Web/Pages/KarZarar.razor           (Gram/GramSayi → Bicim.KarHasGram; uyarı gramı)
?? dotnet/Kuyumcu.Web/Pages/Vezne.razor              (rozet kabına sınıf; id aynı)
?? dotnet/Kuyumcu.Web/wwwroot/css/ortak.css          (vezne rozet kuralları)
?? dotnet/Kuyumcu.Tests/KarZararEkranTestleri.cs     (fikstür özet yanıtı değiştirilebilir; HK30 ×2; bacak testi sabit tr-TR + en-US)
?? dotnet/Kuyumcu.Tests/OrtakGorunumHakemTestleri.cs (HK16)
?? dotnet/Kuyumcu.Tests/Dogrulayici/ORTAK1SinirTestleri.cs (OK3 çelişki düzeltmesi)
```
Migration **gerekmedi**. **Dokunulan sayfalar:** KarZarar, Vezne — ikinci dokunuş gerekçesi: aynı paketin hakem turu bulguları.

## 6. Açık kalanlar (SAHİBİYLE)
1. **P-23 ODEME_KALEMSIZ_YOL = 2** — "TEST-KESIF Kuyumculuk" kiracısının KART (29.09 21:10) ve NAKIT (29.09 23:19) hareketleri → kiracıyı açan şerit (orkestratör tespit etsin); kiracı temizlenince sayaç 0'a döner.
2. **BICIM-TEK (SAHİPSİZ):** has birimi tek karar — `Bicim.Has` "F4 has" ↔ `Bicim.KarHasGram` "N4 g"; ayrıca eksi işareti: `Bicim.Tl` U+2212 "−", gram N-biçimi ASCII "-" (hakem OK6 "-1,25 g" bekliyor). Tek karar orkestratör/BICIM-TEK'te.
3. /vezne 960/1024'te 7 sütunlu tablo kaba (594/658 px) sığmıyor; eylem sütunu (A) kalıbıyla kabın içinde kaydırılarak görünür. Yapışık sütun ölçerde K9 saydığı için geri alındı; daha iyi çözüm (dar genişlikte satırın kart düzenine geçmesi) KATMAN/orkestratör kararı.
4. `kiraci-temizle.sh` bash ile çağrılınca alt tabloları sessizce atlıyor ama tenants satırını SİLİYOR (fail-open; denetim.sh'taki "bash değilse KOŞMA" koruması bu betikte yok) → betik sahibi/orkestratör: başa `[ -n "$ZSH_VERSION" ] || exit 2` koruması.
5. Önceki turlardan aynen açık: KATMAN (genel arama SERBEST-YÜZEN, K10 Secici kesik, P-36c ModalPencere Dispose) · ZO-BELGE (/raporlar/islem-defteri K9) · Cariler rozeti K3 / CUMA-M Escape · OLCEK-1 `td.hucre-vurgu` 9em notu · eşzamanlı derlemeler obj/bin kilitliyor (bu turda da iki kez görüldü: M87 ilk deneme MSB hatası; bir test derlemesi başka şeridin eski derlemesiyle üzerine yazıldı → `touch` + yeniden derleme).

## 7. DoD sözleşmesi
Yeni varlık/kayıt türü, uç, PATCH/DELETE, denetim yolu YOK. Kaydet()/POST yolları, yetki ve yasal kapılar DEĞİŞMEDİ; yalnız gösterim biçimi (has gram) ve bir rozetin yerleşimi değişti. Yeni düğme/form yok. Mevcut id'ler korundu (`vz-toplam-rozet-{id}`, `kz-panel-ozet`); yeni id yok (yalnız sınıf `vz-toplam-rozet`).

## 8. Koşul karşılama (30.09-b)
| Koşul | Durum | Kanıt |
|---|---|---|
| KRİTİK: has kaybı uyarısı 0,00 gram | ✓ | OK8 + HK30_has_kaybi yeşil; canlı "0,004 gram" |
| KRİTİK: BASİT 0,0001 g sıfır gibi | ✓ | OK3 + HK30_kucuk yeşil; canlı "0,0001 g" |
| DÜŞÜK: kültür bağımsızlığı (Bicim merkezi) | ✓ | Bicim.KarHasGram; en-US testleri; en-US tarayıcı; P-36b 0 |
| DÜŞÜK: sabit tr-TR beklenti | ✓ | ORTAK1_HK_bacak "0,66 g" |
| DÜŞÜK: vezne 960 rozet | ✓ (rozet tam görünür; eylem kap içinde kayar — § 6-3) | HK16; vz-olc ölçümü; MUT-B31 |
| DÜŞÜK: P-23 | pakete ait değil → § 6-1 | SQL |
| (A)–(H) = 0, yeni bulgu yok | ✓ (denenen yapışık sütunun ürettiği K9 geri alındı; 30d'de 0) | § 2 |
| Build 0/0 · paket testleri · mutasyon · denetim (paket sayaçları) · yeni kırmızı 0 | ✓ | § 3 (tam süit 7 581/7 581) |
| Test kiracısı silindi, tenants 15→15 | ✓ | § 4 |

## 9. Kurucuya / mali müşavire not
Mevzuat hükmü içeren değişiklik YOK. Has gramının gösterim hanesi yalnız ekrandır; hesap ve sunucu değeri değişmedi.

---

# GEÇMİŞ — 30.09 (gece) devir turu raporu (aynen)

# PAKET ORTAK-1 — Ortak görünüm artıkları · uygulama raporu (DEVİR TURU 30.09 — hakem turu 29.09 bulguları)

**Tarih:** 2026-09-30 00:27–02:10 · önceki turlar 25.09, 27–28.09, 29.09 · **Rol:** Taşkın (backend) + Ertuğ (frontend) · **Şerit:** ORTAK (t6 · 5168/5169 · CDP 9609–9617)
**Durum:** BİTTİ — paket kapısının 7 maddesi güncel ağaçta yeniden sağlandı (kanıtlar aşağıda).
29.09 raporunun tam kopyası: `/private/tmp/kuyumcu-serit/ORTAK-1/c30/rapor-29-09-kopya.md`; 25–29.09 bölümleri bu dosyada "GEÇMİŞ" başlıkları altında aynen duruyor.

## 0. Başlangıç denetimi (ağaç korundu)
- Ağaçtaki iş yeniden yazılmadı. Hakem turunun yedekleri (`dogrulama-yedek/pORTAK-1/{Vezne.razor,cekmece.js,ortak.css}`) ile ağaç `cmp` birebir → **yarım mutasyon yok**.
- Tüm yeni değişiklikler öncesi yedek: `c30/once30/*.once30`.

## 1. Hakem turu 29.09 bulguları — kapatma tablosu

| Bulgu (önem) | Yapılan (dosya) | Kilit test | Mutasyon → kırmızı | Tarayıcı kanıtı (guncel-30/) |
|---|---|---|---|---|
| **[KRİTİK]** /uyum?sekme=risk BASİT: ham anahtar `MasakRisk:Etkin=false` + 3 satır teknik metin | `Uyum.razor`: BASİT `#uy-risk-aciklama` = "Risk puanı yalnız bilgi içindir; hiçbir satışı durdurmaz.", `#uy-risk-durdurmaz` yalnız PRO, `#uy-risk-kapali` BASİT "Risk kuralları şu an kapalı. Açmadan önce mali müşavirinize danışın."; PRO notu da HUB yerine `UyumRiskKapaliPro` (ham anahtar **hiçbir modda** yok). id'ler aynı. | HK1, HK2, O8 (ham anahtar deseni) | M66→HK1 · M67→HK2 · M67b→HK1 · M83→O8+HK2 | E30-uyum-risk-BASIT-acik-1440/-375, E30-uyum-risk-PRO-koyu-1280 (ölçüm: metinler birebir, durdurmaz:false) |
| **[KRİTİK]** /kullanicilar 960 `(siz)` e-posta sütununa biniyor (K9) | Kök neden ölçüldü: `olcek.css` `td.hucre-vurgu:not(.sayi){min-width:9em}` ızgarada içerik minimumunu kapatıyor → sütun 126 px'te kalıyor. `ortak.css`: `html[data-mod] .ana-icerik .veri-tablosu > tbody > tr > td.hucre-vurgu.hucre-tek-satir:not(.sayi){min-width:max-content}` (daha özgül; olcek.css'e dokunulmadı) | HK13, HK14 | M81→HK13 · **tarayıcı MUT-B30**: kural anlık görüntüden çıkarılınca `tdSag 419 < sizSag 437,5` (binme geri) → geri alındı `cmp` birebir | ÖNCE: g/kullanicilar-BASIT-acik-960-uzunad-ONCE (td 419, span 478) · SONRA E30-kullanicilar-BASIT-acik-960 (td 441,5 ≥ span 437,5), E30-kullanicilar-PRO-koyu-1024; tur K9 **0** |
| **[KRİTİK]** /urunler/agac BASİT geliştirici notu + iki birincil | `UrunAgaci.razor`: HUB `KapsamUyarisi` artık okunmaz; BASİT'te not yok, PRO'da tek cümle `#agac-reyon-sayim-notu` ("Reyon sayımı, varsayılan reyonu bu reyon olan ürünleri sayar; reyonda fiilen duran malı değil."). "+ Yeni Reyon" `btn-ikincil` (yeni id `agac-yeni-reyon`) | HK6, HK7, UrunAgaciEkranTestleri.Kapsam_sinirinin_YENI_gercegi (aynı gerçek, yeni metin) | M71→HK6 · M72→HK6 | E30-agac-BASIT-acik-1440 (not:false, tek birincil), E30-agac-PRO-koyu-1280 |
| **[KRİTİK]** /raporlar/kar-zarar dönem kartı iç boşluk 0, sekmelere yapışık; BASİT bacak başlıkları, 4 ondalık gram | `KarZarar.razor`: kök `sayfa dikey-bosluk`, dönem kartı `#kz-donem-kart > .kart-govde`; BASİT başlıklar "Para kârı" / "Altın kârı (has gram)", defter kartı "Defterdeki {maden} gramı", gram BASİT **N2** (yalnız gösterim), PRO N4 aynen; `#kz-has-tanim` ("bacak", "HAS KARŞILIĞI") yalnız PRO | ORTAK1_HK_bacak_basliklari_ve_gram_ondaligi_moda_gore (×2), ORTAK1_HK_donem_karti_ic_boslukta_sekmelerle_arasi_bosluklu | M73, M74, M85 → bacak(basit) · M75, M75b → donem_karti | E30-karzarar-BASIT-acik-1440 (padding 16px 24px; kart altı 438 → sekme üstü 454 = 16 px), -960, E30-karzarar-PRO-koyu-1280 |
| **[KRİTİK]** BASİT jargon artıkları: şubeler "pasife alınamaz", siparişler "işlem uçları" (+kart kenarına taşma), vade "UFUK DIŞI"/"ufuk seçiminden", vezne "Kasa kuyruğu ayrımı zorunlu: kapalı" rozeti | `Isletme.razor` `SonSubeMetni` (BASİT "Tek şube kaldığı için kaldırılamaz — satış yapılacak şube kalmaz.", title + yeni id `s-son-sube-notu`); `Siparisler.razor` BASİT "Sipariş kaydı parayı ve stoğu değiştirmez. Tahsilatı ve teslimi Satış ekranından yapın." + `ortak.css` `.kart > #sip-defter-notu` iç boşluğu; `VadeTakibi.razor` BASİT "Daha ileri vadeli" / "Vadesi seçtiğiniz günlerden sonra gelen borç" / "(bugün itibarıyla — gün seçiminden bağımsız)"; `Vezne.razor` `#vz-ayrim-rozet` yalnız PRO (ayar İşletme ekranında), boş rozet satırının alt boşluğu `ortak.css`'te sıfırlanır | HK5×2, HK8×2, HK9×2, HK10×2, HK12, O8 (hakem jargon deseni: PEP/ufuk/işlem uç/pasif/bacak/ayrımı) | M70→HK5 · M76→HK8 · M77→HK9 · M78→HK10 · M80→HK12 | E30-isletme-subeler-BASIT-acik-1440, E30-siparisler-BASIT-acik-960, E30-vade-BASIT-acik-1280, E30-vezne-BASIT-acik-1440 (rozet:false), E30-vezne-PRO-koyu-375 (rozet:true) |
| **[KRİTİK]** /isletme BASİT üç başlık / iki açıklama / iki birincil | `Isletme.razor`: ikinci `.sayfa-baslik` (h1 + açıklama) BASİT'te çizilmez, PRO aynen; "+ Yeni Şube" `btn-ikincil` (iki modda) | HK3, HK4 | M68→HK3 · M69→HK3+HK4 | E30-isletme-subeler-BASIT-acik-1440 (görünen birincil: yalnız `i-kimlik-duzenle`; h1 0), E30-isletme-subeler-PRO-acik-1280 |
| [DÜŞÜK] Panel `.panel-soru-sayi` / pozisyon uç tutarda 12,5 px | `ortak.css` (B): cok-uzun/en-uzun kademede `font-size:max(1rem, min(…))` + `white-space:normal` (panel ve panel-sade için) | HK11 | M79→HK11 | E30-panel-BASIT-acik-960-stres: `−999.999.999,99 ₺` enjekte → `#panel-soru-satis-deger` ve `#pozisyon-net` **16 px, taşma 0, 1 satır** (önce 12,53 px) |
| [DÜŞÜK] BASİT "PEP" kısaltması | `Sozlukler.OrtakGorunum.cs` `UyumPepSayisiBasit` = "Siyasi nüfuz sahibi olduğunu beyan eden müşteri" (EN "Customers who declared political exposure") | O8, HK1 | M82→O8+HK1 | E30-uyum-risk-BASIT-acik-1440 (kart etiketi) |
| [DÜŞÜK] /cariler ad + "Tedarikçi" rozeti K3 (ölçer artefaktı) | **Alan dışı (Cariler)** — dokunulmadı; § 6'ya sahibiyle yazıldı | — | — | — |
| [DÜŞÜK] (A)–(D) testleri kaynak-dizgesi sözleşmesi | Kabul (bUnit geometri ölçemez). Bu turda geometri tarayıcıda da kilitlendi: tarayıcı MUT-B30 (kullanicilar) ve stres ölçümü (panel). Kapı adımına bağlama önerisi § 6 | — | — | — |

**Tur görüntüsünden bulunup düzeltilen yeni kusurlar (30.09):**
- /vezne `td` "Ayşe Yılmazoğlu" (Hazırlayan) K3 kırık, **16 durum** (960–1440, B/P) — gerçekçi uzun kullanıcı adı tohumlanınca çıktı. `Vezne.razor` hazırlayan hücresi `hucre-tek-satir`. Test HK15, M84 → HK15 kırmızı. Kısmi tur 30b: /vezne K3 **0**.
- /raporlar/kar-zarar BASİT `#kz-has-tanim` "bacak" dili → yalnız PRO (yukarıda, M85).

Dosya başına yeni sözlük anahtarları (TR+EN, konumsal kayıt, eksik dil = derleme hatası): `UyumRiskAciklamaBasit, UyumRiskKapaliBasit, UyumRiskKapaliPro, IsletmeSonSubeBasit, UrunAgaciReyonSayimNotu, KarZararParaKariBasit, KarZararAltinKariBasit, KarZararDefterGramBasit, SiparisDefterNotuBasit, VadeUfukOtesiBasit, VadeUfukOtesiAciklamaBasit, VadeDonemdenBagimsizBasit` (Sozlukler.cs HUB'ına dokunulmadı).

## 2. Kabul ölçümü — tur.mjs başsız Chrome (Release anlık görüntüsü `c30/yayin`, API 5168, vekil 5169)

Tohum: kiracı `PAKET-ORTAK-1-TEST Kuyumculuk 30` (01M3QHPTFBTTJQGTD3EQSQ5QDR): 20 cari (uzun adlar), 8 veresiye (−/6–7 hane), 2 kasa (7.681.959,97 ₺ + USD) + hareket, 2 gider, 3 sipariş, 4 vezne fişi (2'si kuyrukta); sahip kullanıcının adı hakem koşulu için "Ayşe Yılmazoğlu"na çevrildi (PATCH /kullanicilar/ben 200).
- **Tam tur 30** (`c30/tur-SON30`, 22 rota × {BASİT,PRO} × {açık,koyu} × 6 genişlik): kayıt 3 164 · sayfa 528 · sekme 396 · katman 2 240 · hata 0 · geçersiz 0 → analiz `c30/analiz-SON30.txt`.
- **Kısmi tur 30b** (düzeltmeden sonra, 9 rota: /vezne, /kullanicilar, /uyum, /isletme, /urunler/agac, /raporlar/kar-zarar, /siparisler, /raporlar/vade-takibi, /): kayıt 1 023 · hata 0 → `c30/analiz-SON30b.txt`.
  (analiz.py'nin "eksik kombinasyon" listesi 29.09'daki gibi eşik artefaktıdır; her kombinasyonda ölçüm var.)

| Kategori | 25.09 ÖNCE | 29.09 SON | **30.09 SON** |
|---|---|---|---|
| A tablo başlığı / ad hücresi kırık | 196 | 0 | **0** (vezne hazırlayan 16 → 0, 30b) |
| B para kartı taşıyor | 77 | 0 | **0** |
| C filtre satırı hizasız | 8 | 0 | **0** |
| D çekmece sayfa-kaydı | 98 | 0 | **0** |
| E Kim borçlu adı kesik | 9 | 0 | **0** |
| F "Bu reyonda mal yok." | 0* | 0 | **0** |
| G BASİT jargon (ölçer K8 + hakem terimleri) | 24 | 0 (ölçer) | **0** — ölçer K8 ürün metni 0; hakem terimleri (MasakRisk, PEP, ufuk, işlem uçları, pasife alınamaz, bacak, kuyruk ayrımı rozeti) görüntü + ölçüm ile 0 |
| H Yeniden dene ⟂ not | 0* | 0 | **0** |
| K9 /kullanicilar (siz) ⟂ e-posta (hakem) | — | 4 (hakem turu) | **0** |
| sayfa yatay kaydırma | 0 | 0 | **0** (19 ek probta da 0) |

Kalan bulgular (tümü sahipli/gerekçeli, tabanda ya da 29.09'da da vardı): genel arama SERBEST-YÜZEN 375/PRO 768 (KATMAN) · K10 `span.secici-metin` "Altınkaynak (varsayılan)" PRO 768 ve "Tüm kategoriler" PRO 768–1440 (KATMAN/Secici) · / K4 tren 2.3× BASİT 375 (29.09 gerekçesi: veri kaynaklı 5 bölge, katlamak asıl işi gizler) · /isletme K8 "PAKET-ORTAK" (test kiracısının ADI) · /raporlar/islem-defteri K9 kap içi kırpılmış satır ⟂ katlanır (ZO-BELGE; görsel binme yok) · /vezne "Neden değiştirilemez?" ekran-dışı (akış içi `<details>`, 29.09 § D gerekçesi; ölçer yorumu orkestratöre).

**Ekran görüntüleri (hepsi Read ile incelendi)** — `ham/ORTAK-1-ekran/guncel-30/`: E30-uyum-risk-BASIT-acik-1440, -375, E30-uyum-risk-PRO-koyu-1280, E30-isletme-subeler-BASIT-acik-1440, -PRO-acik-1280, E30-agac-BASIT-acik-1440, -PRO-koyu-1280, E30-karzarar-BASIT-acik-1440, -960, -PRO-koyu-1280, E30-siparisler-BASIT-acik-960, E30-vade-BASIT-acik-1280, E30-vezne-BASIT-acik-1440, -PRO-koyu-375, E30-kullanicilar-BASIT-acik-960, -PRO-koyu-1024, E30-panel-BASIT-acik-960-stres, E30-panel-BASIT-acik-1440 (+ tur görüntüleri `c30/tur-SON30/foto`, 1 976 adet).
**Tuval karşılaştırması:** `ham/TUVAL-KARSILASTIRMA/ORTAK-1-Panel-1440-devir-30.png` — Panel 1440'ta bu turda değişen tek şey uç tutar tabanı (1440'ta etkisiz); bölge tablosu 29.09 § D ile aynı (AYNI / gerekçeli fark: kenar menü KATMAN'ın, pozisyon boş durumu, bekleyen iş alt satırı). Değişen diğer ekranların (Uyum, İşletme, Ürün ağacı, Kâr-zarar, Sipariş, Vade, Vezne, Kullanıcılar) tuvali yok (`TUVAL-png` yalnız Ayarlar/Ekranlar/Giris/Musteriler/Panel/Profesyonel/Tezgah); 1440/1280/960/375 görüntüleri yukarıda.

## 3. Testler · mutasyon · denetim
- `dotnet build Kuyumcu.sln --nologo` (son, 01:5x): **0 Hata · 0 Uyarı** (`c30/build8.log`).
- Yeni testler: `OrtakGorunumHakemTestleri.cs` (HK1–HK15; Theory'lerle **22 koşum**) + `KarZararEkranTestleri` 2 yöntem (3 koşum) = **25 yeni koşum**. Güncellenen: `OrtakGorunumSaflik.O8` (29 anahtar, hakem jargon + ham anahtar deseni), `UrunAgaciEkranTestleri.Kapsam_sinirinin_YENI_gercegi_ekranda_yazar` (aynı gerçek, yeni metin/id).
- Filtreli (OrtakGorunum | KarZarar | UrunAgaciEkran | Vezne): **197/197** yeşil (`c30/test4.log`); KarZarar+OrtakGorunum son: **124/124** (`test5.log`).
- **Tam süit** (00:5x, KUYUMCU_TEST_DB=kuyumcu_test_t6, trx `/private/tmp/kuyumcu-serit/trx-pORTAK-1/ortak1-30.trx`): **7 413 · geçti 7 413 · başarısız 0** → paketin yeni kırmızısı **0**. (Tam süit HK15/M85 eklenmeden önce koşuldu; sonraki iki değişiklik yalnız Vezne hücre sınıfı ve KarZarar BASİT dalı — ilgili filtreli koşumlar yeşil.)
- **Mutasyonlar** (`c30/mut.sh`; yedek `c30/mut-yedek/`; her biri geri kopyalandı, `cmp` birebir; 21/21 kırmızı, hayatta kalan **0**; son mutasyondan sonra temiz derleme 0 hata):

| Kod | Değişiklik | Kırmızıya dönen test |
|---|---|---|
| M66 | Uyum BASİT açıklama → HUB | HK1_uyum_risk_BASIT_tek_sade_cumle_ham_anahtar_ve_kisaltma_yok |
| M67 | Uyum PRO kapalı notu → HUB (ham anahtar) | HK2_uyum_risk_PRO_ayrinti_korunur_ham_anahtar_yine_yok |
| M67b | `#uy-risk-durdurmaz` BASİT'te de | HK1 |
| M68 | İşletme ikinci başlık BASİT'te de | HK3_isletme_BASIT_ikinci_baslik_yok_yeni_sube_ikincil_tek_birincil |
| M69 | "+ Yeni Şube" birincil | HK3 + HK4_isletme_PRO_ikinci_baslik_korunur_yeni_sube_yine_ikincil |
| M70 | Son şube notu BASİT → HUB | HK5_…(basit: True) |
| M71 | Ürün ağacı notu BASİT'te de | HK6_urun_agaci_BASIT_gelistirici_notu_yok_tek_birincil |
| M72 | "+ Yeni Reyon" birincil | HK6 |
| M73 | BASİT gram N2 → N4 | KarZararEkranTestleri.ORTAK1_HK_bacak_basliklari_ve_gram_ondaligi_moda_gore(basit: True) |
| M74 | BASİT TL başlığı → HUB | aynı |
| M75 | dönem kartı `kart-govde` kaldır | ORTAK1_HK_donem_karti_ic_boslukta_sekmelerle_arasi_bosluklu |
| M75b | `dikey-bosluk` kaldır | aynı |
| M76 | Sipariş notu BASİT → HUB | HK8_…(basit: True) |
| M77 | Vade BASİT → HUB | HK9_…(basit: True) |
| M78 | Vezne rozeti BASİT'te de | HK10_…(basit: True) |
| M79 | Panel taban `max(1rem` → `max(0rem` | HK11_panel_buyuk_deger_uzun_kademede_1rem_tabanini_asmaz |
| M80 | `#sip-defter-notu` iç boşluk kuralı sil | HK12_siparis_notu_kart_ic_boslugunu_alir |
| M81 | kullanicilar `max-content` → `9em` | HK13_kullanicilar_tek_satir_ad_hucresi_olcek_9em_kuralini_icerikle_ezer |
| M82 | PEP kısaltması geri | O8_basit_metinler_jargon_tasimaz_iki_dil_dolu + HK1 |
| M83 | PRO notuna ham anahtar geri | O8 + HK2 |
| M84 | vezne hazırlayan hücresi sınıfsız | HK15_vezne_hazirlayan_adi_tek_satir_hucrede |
| M85 | `#kz-has-tanim` BASİT'te de | ORTAK1_HK_bacak_…(basit: True) |
| MUT-B30 (tarayıcı) | anlık görüntü ortak.css'te kullanicilar kuralı `9em` | canlı ölçüm: td sağ 419 < "(siz)" sağ 437,5 → binme geri; geri alındı `cmp` birebir, tekrar ölçüm td 441,5 |

- `bash denetim.sh` (00:4x): paketin sayaçları ✓ (ModalPencere, satır içi style, app.css hex, emoji, ODAK_TUZAGI, DUGME-2/OLCEK-1 sayaçları ✓). Tek ✗: **P-23 ODEME_KALEMSIZ_YOL = 1** — canlı DB ölçümü; satır `hareketler 01M3QG1P6EVK9HM5CVRTCBKWXJ` (KART, nakit/veresiye 0), kiracı **"TEST-KESIF Kuyumculuk"** (01M3QFYWC18SPG25CTE5YCAS1H), 29.09 21:10 — bu paketin kiracısı/kodu DEĞİL (ORTAK-1 backend'e dokunmadı). § 6'ya sahibiyle.

## 4. Canlı doğrulama · kiracı · süreçler
- API 5168 = ağacın Release anlık görüntüsü (`dotnet publish -c Release -o c30/yayin`, üç kez: ilk, HK15 sonrası, M85 sonrası); sırlar yalnız süreç ortamına `~/.kuyumcu/*` (600) dosyalarından (`c30/api-baslat.sh`; değer yazdırılmadı). Vekil 5169 (inceleme-vekili.py, fikstür chmod 600). Backend değişikliği yok; HTTP kanıtları: davet 201, kayıt 201, giriş 200, PATCH /kullanicilar/ben 200, sipariş 200 ×3, vezne 200 ×6 (`c30/*.py` çıktıları).
- Kiracı silme: `kiraci-temizle.sh 01M3QHPTFBTTJQGTD3EQSQ5QDR` raporu **3 072 satır + tenants 1** → `--uygula` rc 0. Doğrulama: `tenants WHERE id=…` = **0**; `REPLACE(ad,'-','') LIKE 'PAKETORTAK1%'` = **0**. Davet artığı `kiraci_davetleri` ORTAK-1 süzgeci **3 satır silindi** (ROW_COUNT 3): kendi davetim + **hakem turunun iki davet artığı** ("PAKETORTAK-1-HAKEM-TEST …", 29.09 20:55/20:58; kiracıları zaten yoktu) — ⚠ bu iki satır hakemin açtığıydı; aynı paketin artığı olduğu ve kiracısız kaldığı için temizlendi, açıkça bildiriyorum. Sonrası = 0. QA-TEST-* dört kiracı yerinde, dokunulmadı.
- tenants: başlangıç **14**, son **14**.
- 5168 ve 5169 durduruldu (`lsof` boş); 5199/5200'e dokunulmadı; başsız Chrome süreçleri kapandı.

## 5. Değişen dosyalar (bu tur; `git status --porcelain | grep -v -E "/(bin|obj)/"` — hepsi izlenmeyen ağaçta)
```
?? dotnet/Kuyumcu.Core/I18n/Sozlukler.OrtakGorunum.cs      (12 yeni anahtar + PEP metni)
?? dotnet/Kuyumcu.Web/wwwroot/css/ortak.css                (panel tabanı, sipariş notu, rozet satırı, kullanicilar min-width)
?? dotnet/Kuyumcu.Web/Pages/Uyum.razor · Isletme.razor · UrunAgaci.razor · KarZarar.razor · Siparisler.razor · VadeTakibi.razor · Vezne.razor
?? dotnet/Kuyumcu.Tests/ — OrtakGorunumHakemTestleri.cs (yeni), OrtakGorunumTestleri.cs (O8), KarZararEkranTestleri.cs (+2), UrunAgaciEkranTestleri.cs (1 test güncellendi)
```
Migration **gerekmedi**; backend değişikliği YOK. **Dokunulan sayfalar:** Uyum, Isletme, UrunAgaci, KarZarar, Siparisler, VadeTakibi, Vezne — tekrar dokunuş **gerekçesi:** aynı paketin hakem turu bulguları (başka dalga değil). Kullanicilar.razor ve Panel.razor bu turda değişmedi (çözüm ortak.css'te).

## 6. Açık kalanlar (SAHİBİYLE)
1. **P-23 ODEME_KALEMSIZ_YOL = 1** — "TEST-KESIF Kuyumculuk" kiracısının KART hareketi (29.09 21:10) → keşif/test kiracısını açan şerit (orkestratör tespit etsin); kiracı temizlenince sayaç 0'a döner.
2. KATMAN: genel arama SERBEST-YÜZEN 375/PRO 768; K10 Secici kesik ("Altınkaynak (varsayılan)", "Tüm kategoriler").
3. ZO-BELGE: /raporlar/islem-defteri K9 (kap içi kırpılmış satır ⟂ katlanır; görsel binme yok).
4. Cariler şeridi / orkestratör: /cariler ad + "Tedarikçi" rozeti K3 (ölçer artefaktı) — rozeti taban çizgisine oturtmak (ORTAK-1'in #k-siz çözümü gibi `font-size: inherit` ya da `vertical-align: baseline`) ya da ölçerde rozeti satır sayımından hariç tutmak.
5. OLCEK-1 (bilgi): `olcek.css` `td.hucre-vurgu:not(.sayi){min-width:9em}` ızgaraya dönüşen tablolarda içerik minimumunu kapatır; ORTAK-1 yalnız `hucre-tek-satir` hücreler için ezdi. Başka tek satır ad hücreleri aynı kalıba girerse OLCEK-1 kuralı `:not(.hucre-tek-satir)` ile daraltılabilir.
6. BICIM-TEK (SAHİPSİZ): has birimi tek karar (N4 g ↔ Bicim.Has). Bu tur yalnız BASİT gösterimi N2'ye indirdi; PRO N4 aynen.
7. Orkestratör: (a) kaynak-dizgesi sözleşme testleri (O2–O5, HK11–13) davranışı tek başına kanıtlamaz — tarayıcı ölçümü (tur.mjs / stres probu) kapı adımına bağlanmalı; (b) ölçer, akış içi `<details>` açılınca pencere altına uzayan içeriği "ekran-dışı katman" sayıyor (/vezne); (c) eşzamanlı derlemeler obj/bin kilitliyor — tam süit şeritler durunca ölçülmeli.
8. Önceki turlardan aynen açık: P-36c ModalPencere Dispose → KATMAN; Cariler BASİT çekmece Escape → CUMA-M.

## 7. DoD sözleşmesi
Yeni varlık/kayıt türü, uç, PATCH/DELETE, denetim yolu YOK. Kaydet()/POST yolları, yetki ve yasal kapılar DEĞİŞMEDİ. Yeni form/düğme yok (iki düğmenin yalnız sınıfı birincil→ikincil; tıklama → aynı modal, UX1C4/HK testleri). Mevcut id'ler korundu (`uy-risk-*`, `s-yeni-sube-ac`, `kz-bas/kz-bit`, `sip-defter-notu`, `vz-ayrim-rozet`); yeni id'ler `agac-yeni-reyon`, `agac-reyon-sayim-notu`, `s-son-sube-notu`, `kz-donem-kart`.

## 8. Koşul karşılama (30.09)
| Koşul | Durum | Kanıt |
|---|---|---|
| Hakem KRİTİK: uyum risk ham anahtar/teknik metin | ✓ | § 1; HK1/HK2; E30-uyum görüntüleri |
| Hakem KRİTİK: kullanicilar (siz) K9 960 | ✓ | tur K9 0; MUT-B30; E30-kullanicilar |
| Hakem KRİTİK: ürün ağacı notu + iki birincil | ✓ | HK6/HK7; E30-agac |
| Hakem KRİTİK: kâr-zarar kart boşluğu, BASİT başlık, gram | ✓ | ölçüm 16px 24px, 16 px aralık; ORTAK1_HK testleri |
| Hakem KRİTİK: BASİT jargon (şube, sipariş, vade, vezne) | ✓ | HK5/8/9/10; görüntüler |
| Hakem KRİTİK: işletme başlık/açıklama/birincil | ✓ | HK3/HK4; ölçüm (birincil yalnız i-kimlik-duzenle, h1 0) |
| DÜŞÜK: panel tabanı · PEP | ✓ | stres 16 px; O8/HK1 |
| DÜŞÜK: cariler rozeti | alan dışı → § 6-4 | — |
| DÜŞÜK: kaynak-dizgesi testleri | kabul + tarayıcı mutasyonu | § 3, § 6-7 |
| (A)–(H) = 0, yeni bulgu yok | ✓ (yeni 16 durumluk vezne K3 bu turda düzeltildi; kalanlar sahipli) | § 2 |
| Build 0/0 · paket testleri · mutasyon · denetim (paket sayaçları) · yeni kırmızı 0 | ✓ | § 3 |
| Test kiracısı silindi, tenants 14→14 | ✓ | § 4 |

## 9. Kurucuya / mali müşavire not
Mevzuat hükmü içeren değişiklik YOK. Risk ekranındaki "açmadan önce mali müşavirinize danışın" cümlesi mevcut kuralın (MasakRisk kapalı, açılması teyide bağlı) sade anlatımıdır; eşik/hüküm eklenmedi.

---

# GEÇMİŞ — 29.09 devir turu raporu (aynen)

## PAKET ORTAK-1 — Ortak görünüm artıkları · uygulama raporu (DEVİR TURU 29.09 — kıyas devri + son doğrulama bulguları)

**Tarih:** 2026-09-29 (devir turu 3) · önceki turlar 25.09, 27–28.09 · **Rol:** Taşkın (backend) + Ertuğ (frontend) · **Şerit:** ORTAK (t6 · 5168/5169)
**Durum:** BİTTİ — paket kapısının 7 maddesi güncel ağaçta (29.09 22:00–23:10) yeniden sağlandı; kanıtlar aşağıda.
28.09 raporunun tam kopyası: `/private/tmp/kuyumcu-serit/ORTAK-1/c29/rapor-28-09-kopya.md`; 25–28.09 bölümleri bu dosyada § 7 sonrası "GEÇMİŞ" olarak aynen duruyor.

## A. 29.09 turunda ne yapıldı (özet)

1. Ağaçtaki iş **korundu, yeniden yazılmadı**. 29.09 15:50–18:25 arasındaki (kesilen) koşumun değişiklikleri ağaçta bulundu ve doğrulandı:
   `c29/*.once29` yedekleri ↔ ağaç farkı okundu; **yarım mutasyon kalıntısı YOK** — `c29/mut-yedek/` altındaki her dosyanın en son yedeği ağaçla `cmp` birebir
   (Kullanicilar M54 farkı = sonradan bilinçli eklenen `hucre-tek-satir`, mutasyon değil). `// MUT` işareti 0.
2. **SON DOĞRULAMA bulguları kapatıldı:**
   - **[ORTA] (G) /vezne BASİT "Vezne" türevleri** — `Vezne.razor` artık `VzDurumVeznede/VzVezneyeGonder/VzVeznelendi/…` özellikleriyle `Dil.BasitMi` dalı taşıyor
     (satır 1065–1072; süzgeç seçeneği satır 41, düğme satır 239, bant satır 820). BASİT metinleri `Sozlukler.OrtakGorunum.cs` (TR+EN):
     "Kuyrukta", "Kasa kuyruğuna gönder", "Kasa kuyruğuna gönderildi"… Canlı (29.09 22:58, BASİT 1280): süzgeç "Kuyrukta", rozet "Kuyrukta", başlık "Kasa kuyruğu"
     (`ham/ORTAK-1-ekran/guncel-29/D29-vezne-BASIT-acik-1280.png`). Tur (24 kombinasyon) /vezne K8 jargon = **0**. PRO modda "Vezne/Veznede" korunur (muhasebe dili; D10).
     Kilit: `G5_vezne_BASIT_modda_vezne_koku_ve_cekimleri_gorunmez`, `G7_vezne_BASIT_metinleri_kokten_arinmis_iki_dil` (M34–M39, M42 → kırmızı).
   - **[ORTA] 'PAKETORTAK-1-TEST Dogrulama' kiracısı** (01M3CDAZVMHGYSJ9F28VBA3JBG) — 29.09 21:5x `SELECT … FROM tenants`: **yok** (silinmiş). Temizlik sorgusu artık
     tireden bağımsız: `REPLACE(ad,'-','') LIKE 'PAKETORTAK1%'` → **0** (tenants), `kiraci_davetleri` → **0**.
3. **KIYAS devri (ham/KIYAS-BULGULARI-2026-09-28.md § "ORTAK-1") satır satır** — § B.
4. **Tam tur yeniden koşuldu** (22 rota × {BASİT,PRO} × {açık,koyu} × {1440,1280,1024,960,768,375}; 3 232 kayıt; hata 0, geçersiz 0) ve iki YENİ kusur bulunup
   düzeltildi (§ C). Son tur: (A)–(H) **0**, sayfa-kaydı **0**.

## B. Kıyas devri — ORTAK-1 satırları (güncel ağaç, gerçek tarayıcı)

| # (KIYAS) | Devredilen | Yapılan (dosya:satır) | Kilit test | Mutasyon | Tarayıcı kanıtı |
|---|---|---|---|---|---|
| 4/5 | Çift başlık (UstBar h1 + SayfaBasi h2) | `Bilesenler/SayfaBasi.razor` `UstBardaAyni` (üst barın `RotaBasliklari` adıyla eşit/kapsıyorsa `sayfa-basi--baslik-ustte`) + `UstBardaGizle` parametresi (farklı sözcükle tekrar: Vezne, VadeTakibi, Siparisler, UrunEtiketleri); `ortak.css` şerit başlığı `display:none` (DOM'da, id aynı). UstBar'a dokunulmadı. | KY4, KY4b, KY5, KY5b, KY5c, KY9×4 | M45, M46, M52, M47/M47b, M55 | D29-kullanicilar/vezne/urunler görüntüleri: tek başlık |
| 6 | Panel "Bekleyen iş" aynı başlıklı satırlar | `Pages/Panel.razor` `IsAyrintisi()` — bildirim `IlgiliVarlikTuru=Cari` ise alt satır: müşteri adı (kartına bağlantı) · tutar · gün; veri panelin zaten yüklediği vade raporundan (yeni istek yok; rapor yetkisi yoksa ad gösterilmez) | KY1, KY2 | M43, M43b, M56 | D29-panel-BASIT-acik-1440 (4 satır ayırt edilebilir: Anadolu Holding A.Ş. 179.999,94 ₺ …) |
| 6 | Panel yükleyici yakalama boşluğu (`catch ApiHata or JsonException`) | `Panel.razor` `YukleHatasi/AgHatasi/HataMetni`: `HttpRequestException` + `TaskCanceledException` da yakalanır, kutucuk bandına `PanelBaglantiHatasi` yazılır (11 yükleyici) | KY3 | M44 | — (ağ kesintisi bUnit ile) |
| 6/UX-5 | Panel ⛔/⚠ emojileri | `<Ikon Ad="@Ikonlar.Uyari" />` (4 yer) | KY7(Panel) | M51 | — |
| 8 | KarZarar karşılaştır yerel `<select>` | `Pages/KarZarar.razor` ortak `Secici` (id `kz-karsilastir` aynı; değerler/`Yukle()` aynı); `KarZararEkranTestleri` gerçek kullanıcı yolu (tetik → seçenek) | Karsilastir_KpiKartindaDeltaBasar, P35_03, P64_01, P64_02 | M49 | D29-karzarar-PRO-acik-1280 |
| 8 | StatKart renkli sol şerit | `ortak.css` `.kart.stat-kart{,.yesil,.kirmizi,.mavi} { border-left: 1px solid var(--ayrac) }` (app.css'e dokunulmadı) | KY6 | M48 | D29-karzarar-PRO-acik-1280 (düz kenar) |
| 8 | Has birimi `N4 g` ↔ `F4 has` | **YAPILMADI — gerekçe:** `Bicim.cs` ortak servis (SAHİPSİZ, BICIM-TEK önerisi) ve kıyas belgesi "ekip oyuyla" diyor; KarZarar'da tek sayfada değiştirmek iki ekranda üçüncü bir biçim doğurur. Açık kalan § G-3. | — | — | — |
| 4 | /urunler "Durum" hizası (1280/375) | Ölçüldü: tur K7 /urunler **0** (24 kombinasyon); 375'te Durum etiketi + seçici + "Süzgeç" tam genişlik, hizalı | O4b (27.09) | M-26 | D29-urunler-BASIT-acik-1280/-375 |
| UX-5 | Emoji: GiderPusulalari 4, UrunAgaci 4, Uyum 2, UrunEtiketleri 1 | Hepsi `Ikon` ya da metin (yorum dışı emoji **0**); UrunAgaci sözlük metnindeki "⚠" HUB'a dokunmadan `TrimStart` + Ikon | KY7×5 | M50 (Uyum), M51 (Panel) | — |

Alan dışı kıyas satırlarına (1 tema, 2 kenar menü, 3 Raporlar, 5 Kasalar emojisi, 7 Cariler, 9 Ekranlar, denetim.sh sayaç kapsamı) **dokunulmadı**.

## C. 29.09 tur ölçümünde bulunan ve düzeltilen kusurlar

| # | Kusur (ölçüm) | Düzeltme | Test | Mutasyon | Canlı prob |
|---|---|---|---|---|---|
| C-1 | /kullanicilar `td.hucre-vurgu "Ortak Sahip (siz)"` K3 kırık, **10 durum** (B/P 960–1440). Prob: `white-space:nowrap` etkin, ama "(siz)" 12–14 px küçük yazı farklı üst kenara oturuyor → metin kutusu iki "satır" (göze taban kayması) | `ortak.css`: `.tablo-sar td > #k-siz { font-size: inherit; }` (renk soluk kalır) | KY11 | M64 → KY11 kırmızı | tops `[73]` tek satır, `spfs 14px = tdfs` |
| C-2 | Panel BASİT 960 `#panel-soru-satis-yok "Bugün henüz satış yok."` K3 kırık (2 durum). Prob: alt satır iç genişliği 157 px, metin 128+28 px + boşluk | `ortak.css`: `@container (max-width:170px) { .panel-sade .panel-soru > .panel-soru-alt { font-size: calc(var(--yazi-govde)*.7778) } }` — yalnız dar kartta; 1440 tuval boyu değişmez | KY12 | M65 → KY12 kırmızı | satır 2 → **1**, 15 px → 14 px (D29-panel-BASIT-acik-960) |

Önceki (kesilen) 29.09 koşumunun düzelttikleri (ağaçta, bu turda tur ile doğrulandı): Vezne eylem hücresi `<td class="rozet-satir">` → iç `div.vz-eylem-hucre` (K9 binme; KY10, M57–M61, M63),
Gider form kart iç boşluğu + ad hücreleri (KY8, M53, M54, M62), Siparisler tür / Bildirimler iş adı / Kullanicilar e-posta-şube `hucre-tek-satir` (KY8, KY10, M58, M59),
`EtiketEkranTestleri.cs` xUnit1031 uyarısı (`async Task` + `await DisposeComponentsAsync()`) → derleme **0 uyarı**.

## D. Kabul ölçümü — 29.09 son tur (tur.mjs başsız Chrome, CDP 9609–9612, vekil 5169 → API 5168, Release anlık görüntüsü `c29/yayin`)

Ham: `/private/tmp/kuyumcu-serit/ORTAK-1/c29/tur-SON29f/` · analiz: `ham/ORTAK-1-tur/analiz-SON-29-tam.txt` · kategori karşılaştırma: `ham/ORTAK-1-tur/karsilastirma-29b-29f.txt`.
528 sayfa + 396 sekme + 2 308 katman (1 044 açılan), hata 0, geçersiz 0, 24 kombinasyon (analiz.py "eksik" listesi eşik 42/rota-sayısı artefaktıdır; her kombinasyon 22 sayfa).

| Kategori | 25.09 ÖNCE | 27.09 SON | **29.09 SON** |
|---|---|---|---|
| A tablo başlığı / ad hücresi kırık | 196 | 0 | **0** |
| B para kartı taşıyor | 77 | 0 | **0** |
| C filtre satırı hizasız | 8 | 0 | **0** |
| D çekmece sayfa-kaydı (açılış adedi >40 px) | 98 | 0 | **0** |
| E Kim borçlu adı kesik | 9 | 0 | **0** |
| F "Bu reyonda mal yok." kırık | 0* | 0 | **0** |
| G BASİT jargon (uyum risk, şubeler, vezne) | 24 | 0 | **0** (/vezne dahil) |
| H Yeniden dene ⟂ not | 0* | 0 | **0** |
| sayfa yatay kaydırma | 0 | 0 | **0** (14 ek görüntüde de 0) |

**Rota × genişlik × mod — kalan bulgular (tümü; "SERBEST-YÜZEN genel arama" hariç, o her rotada KATMAN'ın):**

| Rota | Bulgu | Durumlar | Karar |
|---|---|---|---|
| `/` | K10 `span.secici-metin` "Altınkaynak (varsayılan)" | P 768 | KATMAN (Secici*) — tabanda (TAM-TUR-v3) da var |
| `/` | K4 tren 2.3×5 | B 375 | GEREKÇELİ — veri kaynaklı: 5 kart (satış, kasa, pozisyon, 3 borçlu, 4 iş) telefonda alt alta 1 869 px; tuvalin 5 bölgesi korunur, katlamak "zorunlu önce" kuralına göre bu ekranın asıl işini gizler. Bekleyen iş alt satırı katkısı ≈160 px (0,2×) — onsuz da 2,1× |
| `/isletme` | K8 "PAKET-ORTAK" | B hepsi | test kiracısının ADI (ürün metni değil) |
| `/raporlar/islem-defteri` | K9 tablo hücresi ⟂ "Listedeki bir işlem yanlışsa" | B 1024–1440, P 1024/1440 | ZO-BELGE — kap içinde dikey kayan tablonun KIRPILMIŞ satırları ölçerde kesişiyor (görüntüde binme yok: `D29`/`c29/d/islem-defteri-1280.png`); sayfa ve katlanır ZO-BELGE'nin (Sozlukler.ZoBelge `IslemDuzeltmeBaslik`) |
| `/urunler` | K10 "Tüm kategoriler" | P 768–1440 | KATMAN (Secici*) — tabanda var |
| `/vezne` | "Neden değiştirilemez?" ekran-dışı | tüm genişlik/mod | GEREKÇELİ — bkz. alt not |

*Vezne ekran-dışı notu:* ölçer `details[open] > :not(summary)` öğesini "katman" sayıyor; bu öğe **akış içindedir** (prob: `position static/static`), açılınca tablo satırını uzatır,
sayfa normal kaydırmayla erişir, yatay kaydırma 0. 1280 BASİT probu (`D29-vezne-BASIT-acik-1280-kilit-acik.png`): açık gerekçe kutusu 760 px'te biter (pencere 800),
kaydırma gerekmedi, kutu tam okunuyor. Tur ekran-dışı sayıyor çünkü özet satırı pencerenin altında açılıyor (kutu altı > innerHeight). Kutuyu sabit/yüzen katmana çevirmek
tam da ölçerin yakalamak istediği "serbest yüzen" kusurunu üretir; açılınca sayfayı kaydırmak da "sayfa-kaydı" üretir. Bu nedenle akış içi katlanır bırakıldı —
orkestratörün ölçer yorumu için § G-5'e yazıldı. (Katlanır kutu 28.09'da bu paketin getirdiğidir: kilit kutusu satırı ~300 px uzatıyordu, G10 testi.)

**Tabanla (TAM-TUR-v3) satır satır:** tabandaki (A)–(H) satırları (Panel `#panel-soru-kasa-deger` taşma, Panel `panel-liste-ad` kesik, /vezne K8 "Vezne", /kullanicilar K9 Yeniden dene,
islem-defteri `p.stat-deger` taşma) → **0**. Tabanda olup sürenler yalnız KATMAN'ın K10 Secici ve genel arama satırları.
Yeni ve alanımda olan tek satır grubu C-1/C-2 → düzeltildi. Kalan "yeni" görünenler yukarıdaki tabloda sahibiyle/gerekçesiyle.

**Ekran görüntüleri (hepsi Read ile incelendi)** — `ham/ORTAK-1-ekran/guncel-29/` (29.09 22:58–23:00, cache kapalı, yatay kaydırma 0):
D29-panel-BASIT-acik-1440, D29-panel-BASIT-acik-960, D29-panel-BASIT-koyu-375 (tam sayfa), D29-panel-PRO-acik-1280, D29-kullanicilar-BASIT-acik-1024,
D29-kullanicilar-PRO-koyu-1440, D29-vezne-BASIT-acik-1280, D29-vezne-BASIT-acik-1280-kilit-acik, D29-vezne-PRO-koyu-375, D29-urunler-BASIT-acik-1280,
D29-urunler-BASIT-acik-375, D29-siparisler-BASIT-koyu-960, D29-gider-BASIT-acik-960, D29-uyum-risk-BASIT-acik-1280 (+ kesilen koşumun ONCE/SON 1280 görüntüleri).

**Tuval karşılaştırması (Panel 1440×900, BASİT açık):** `ham/TUVAL-KARSILASTIRMA/ORTAK-1-Panel-1440-devir-29.png` (tuval | bizde).

| Bölge | Tuvalde | Bizde | Durum |
|---|---|---|---|
| Üst şerit, "Yeni satış" | aynı yerleşim | aynı | AYNI |
| Kenar menü | grup başlıkları + açık "Günlük iş" | kapalı grup özeti | GEREKÇELİ FARK — KATMAN şeridinin (KenarCubugu), ORTAK-1 dışı |
| 3 kart değer boyu | 36 px Sora | 36 px (min() özgün boyu seçer) | AYNI |
| 3 kartın yüksekliği | içerik kadar | pozisyon kartındaki boş durum (altın yok) satırı yüksek | GEREKÇELİ FARK — gerçek veri (kiracıda altın yok), boş durum metniyle |
| Kasa alt satırı | "Nakit … Banka …" | "Nakit …" / "Dolar kasalar …" | GEREKÇELİ FARK — döviz kasası verisi; sarar, taşmaz |
| Kim borçlu ızgarası | ad · durum · tutar · eylem | aynı; uzun ad 2 satır | AYNI / (E) koşulu |
| Bekleyen iş | başlık · kaynak etiketi | başlık · kaynak + **müşteri · tutar · gün** alt satırı | GEREKÇELİ FARK — kıyas bulgusu 6 (aynı başlıklı satırlar ayırt edilemiyordu) |
| Kart kenarı | ince tek renk | ince tek renk (StatKart şeridi nötr) | AYNI |

**TARAYICI MUTASYONU (29.09, güncel anlık görüntü):** `yayin/wwwroot/index.html`'den ortak.css bağlantısı silindi (yedek `c29/mut-yedek/index.html.MUTA29`) →
Panel BASİT 960: `styleSheets` içinde ortak.css **yok**, taşan kart değeri **1** (B geri geldi). Geri: `cmp` birebir, sunulan index.html'de bağlantı var.
27.09 MUT-A (A 26 · B 13 · C 1 · E 2 geri) ve MUT-B (37 sayfa-kaydı geri) geçerli: `cekmece.js` (25.09) ve `Cekmece.razor` (27.09) o günden beri DEĞİŞMEDİ; 29.09 turunda sayfa-kaydı 0.

## E. Testler · mutasyon · denetim

- `dotnet build Kuyumcu.sln --nologo` (29.09 23:0x, son): **0 Hata · 0 Uyarı** (`c29/build-son3.log`). Ara bir derleme (22:15) başka şeridin eşzamanlı derlemesi yüzünden
  `MSB4018 IOException … obj/Debug … cannot access the file` ile düştü — ortam yarışı; tekrarında temiz.
- Paket testleri: `OrtakGorunumTestleri.cs` 36 + `OrtakGorunumKiyasTestleri.cs` 15 test yöntemi (bugün +2: KY11, KY12) → filtre `OrtakGorunum` **58/58 yeşil**
  (+ `KarZararEkranTestleri`, `EtiketEkranTestleri` tam süitte yeşil).
- **Tam süit (29.09 23:0x, temiz derleme, KUYUMCU_TEST_DB=kuyumcu_test_t6, trx `/private/tmp/kuyumcu-serit/trx-pORTAK-1`): 7 362 test · 7 362 geçti · 0 başarısız.**
  Bir önceki koşum (22:15, derleme yarışı sırasında): 7 332 · 1 başarısız `Dogrulayici.SATIS3SinirTestleriK2Hasim2.K2H_doviz_girildi_kur_yok_satir_SESSIZCE_DUSMEZ`
  (SATIS-3 hakeminin 22:05'te değiştirdiği dosya; son koşumda yeşil) → paketin yeni kırmızısı **0**.
- **Mutasyonlar** (`perl -0777 -pi`, yedek `c29/mut-yedek/`, her biri geri kopyalanıp `cmp` birebir; son mutasyondan sonra temiz derleme 0 hata):

| Kod | Değişiklik | Kırmızıya dönen test |
|---|---|---|
| M34–M39, M42 (28.09) | Vezne BASİT metinleri PRO'ya / sözlükte "vezne" kökü | G5_vezne_BASIT_modda_vezne_koku_ve_cekimleri_gorunmez (+G7) |
| M40 / M41 (28.09) | vezne düğme sınıfı / kart notu iç boşluğu | G8_… / G9_… |
| M35b, M42b | vezne BASİT dalı / sözlük (29.09 tekrar) | G5 (+G7) |
| M43, M43b, M56 | Panel bekleyen iş alt satırı | KY1, KY2 / KY1 |
| M44 | Panel ağ hatası yakalama | KY3_bildirim_ag_hatasinda_iskelet_sonsuz_donmez_banda_yazar |
| M45, M46, M52 | SayfaBasi başlık eşleme / içerme / sınıf | KY5c / KY5b / KY4+KY5b+KY5c |
| M47, M47b | ortak.css çift başlık gizleme | KY4b |
| M48 | StatKart şerit nötr | KY6 |
| M49 | KarZarar Secici → select | KarZararEkranTestleri ×4 (Karsilastir_KpiKartindaDeltaBasar, P35_03, P64_01, P64_02) |
| M50 / M51 | Uyum / Panel emoji geri | KY7(Uyum) / KY7(Panel) |
| M53, M54, M62 | Gider kart boşluğu / hücre sınıfları | KY8 |
| M55 | Vezne UstBardaGizle | KY9(Vezne) |
| M57–M61, M63 | vezne eylem hücresi / tek satır hücreler / summary rengi | KY10 |
| **M64** | `#k-siz font-size: inherit → smaller` | **KY11_kullanicilar_siz_rozeti_tabloda_ad_ile_ayni_boyda** |
| **M65** | `@container (max-width:170px) → 1px` | **KY12_panel_dar_kart_alt_satiri_kap_sorgusuyla_kuculur_genis_kartta_degismez** |

Hayatta kalan mutasyon: **0**. (25.09/27.09 tablosu — M-ORTAK1-1 … 33 — geçmiş bölümünde aynen.)

- `bash denetim.sh` (29.09 22:1x): **DENETİM TEMİZ.** record 3 (tavan 3) · app.css ham hex 25 (tavan 25) · satır içi style 72 (tavan 196) · ModalPencere 48 (tavan 55) ·
  Layout+Bilesenler emoji 0 · P-36c ODAK_TUZAGI 0.

## F. Canlı doğrulama, kiracı, süreçler

- API 5168: ağacın Release anlık görüntüsü (`dotnet publish -c Release -o c29/yayin`, 29.09 21:5x), sırlar yalnız süreç ortamına `~/.kuyumcu/*` (600) dosyalarından
  (`c29/api-baslat.sh`; değer yazdırılmadı). Vekil 5169 (inceleme-vekili.py, fikstür chmod 600). ortak.css değişikliği anlık görüntüye kopyalandı, `cmp` birebir.
- Kiracı `PAKET-ORTAK-1-TEST Kuyumculuk 29` (01M3PVF2ZKC7CA62KWQ476BMZT; 29.09 kesilen koşumda ürün uçlarıyla tohumlandı: 30 cari, veresiye, 2 kasa + hareket,
  gider, 3 sipariş, vezne fişleri). Silmeden önce `kiraci-temizle.sh` raporu: **3 219 satır + tenants 1** (ör. vezne_fisleri 4, siparisler 3, kasalar 2, kasa_hareketleri 4) → `--uygula` ile silindi;
  davet satırı `kiraci_davetleri` 1 satır silindi (ROW_COUNT 1).
- Doğrulama: `tenants WHERE REPLACE(ad,'-','') LIKE 'PAKETORTAK1%'` = **0**, `kiraci_davetleri` aynı süzgeç = **0**. tenants: oturum başında **16**, sonda **14**
  (fark başka şeritlerin kendi kiracılarını silmesi/açması; QA-TEST-* dördü yerinde, dokunulmadı).
- 5168 ve 5169 durduruldu (`lsof` ikisi de boş); 5199/5200'e dokunulmadı; başsız Chrome süreçleri kapandı.

## G. Açık kalanlar (SAHİBİYLE)

1. KATMAN: genel arama katmanı 375/PRO 768 "SERBEST-YÜZEN"; K10 `span.secici-metin` kesik ("Altınkaynak (varsayılan)" PRO 768, "Tüm kategoriler" PRO 768–1440).
2. ZO-BELGE: /raporlar/islem-defteri K9 — kap içi kaydırılan tablonun kırpılmış satırları "Listedeki bir işlem yanlışsa" katlanırıyla ölçerde kesişiyor (görsel binme yok).
3. BICIM-TEK (SAHİPSİZ): has birimi `N4 g` (KarZarar) ↔ `F4 has` (Bicim.Has) tek karar — ekip oyu gerekir; ORTAK-1 tek sayfada değiştirmedi.
4. SAHİPSİZ (kıyas § Sahipsiz): TEMA-TEK, ISTEK-2 (ApiIstemci ağ/zaman aşımı → ApiHata; Panel bu paketle yerel olarak kapandı), VADE-BILDIRIM metni, UX-5-GENIS (denetim.sh emoji sayacı Pages/ kapsamı).
5. Orkestratör (ölçer yorumu): akış içi `<details>` açılınca pencere altına uzayan içerik "ekran-dışı katman" sayılıyor (/vezne kilit gerekçesi). Öneri: `ekstra.js` ekranDisi hesabında
   `position: static` katlanır içeriği hariç tutmak ya da açmadan önce özet satırını ortalamak.
6. Eşzamanlı derlemeler `obj/`/`bin/` dosyalarını kilitliyor/siliyor (bugün 1 derleme MSB4018) — orkestratör kapısı tam süiti şeritler durunca ölçmeli.
7. 28.09 raporundaki diğerleri (P-36c ModalPencere Dispose → KATMAN; Cariler BASİT çekmece Escape → CUMA-M) aynen açık. (EtiketEkranTestleri xUnit1031 → bu turda KAPANDI.)

## H. Değişen dosyalar (bu paketin; `git status --porcelain | grep -v -E "/(bin|obj)/"`)

```
 M dotnet/Kuyumcu.Web/Bilesenler/StatKart.razor
 M dotnet/Kuyumcu.Web/Pages/Kullanicilar.razor
 M dotnet/Kuyumcu.Web/Pages/Panel.razor
 M dotnet/Kuyumcu.Web/wwwroot/index.html
?? dotnet/Kuyumcu.Core/I18n/Sozlukler.OrtakGorunum.cs
?? dotnet/Kuyumcu.Web/Bilesenler/DegerBoyu.cs
?? dotnet/Kuyumcu.Web/wwwroot/css/ortak.css
?? dotnet/Kuyumcu.Tests/ (dizin izlenmiyor) — OrtakGorunumTestleri.cs, OrtakGorunumKiyasTestleri.cs, KarZararEkranTestleri.cs (Secici yolu), EtiketEkranTestleri.cs (xUnit1031)
izlenmeyen ağaçta, değişikliği bu pakette: Bilesenler/SayfaBasi.razor, Bilesenler/Cekmece.razor, Bilesenler/AltinPozisyonuKarti.razor, wwwroot/js/cekmece.js,
Pages/Vezne.razor, Pages/Uyum.razor, Pages/Isletme.razor, Pages/UrunAgaci.razor, Pages/GiderPusulalari.razor, Pages/Gider.razor, Pages/Siparisler.razor,
Pages/Bildirimler.razor, Pages/KarZarar.razor, Pages/VadeTakibi.razor, Pages/UrunEtiketleri.razor
```
29.09 bu turda (kesilen koşumdan sonra) değişen: `wwwroot/css/ortak.css` (C-1, C-2), `Kuyumcu.Tests/OrtakGorunumKiyasTestleri.cs` (KY11, KY12). Migration **gerekmedi**, backend değişikliği YOK.
**Dokunulan sayfalar (tek dokunuş):** 25–28.09 listesi + 29.09: Bildirimler, Gider, KarZarar, VadeTakibi, UrunEtiketleri (ilk dokunuş, hepsi paketin dosya alanında);
Panel, Vezne, Kullanicilar, Siparisler, Uyum, UrunAgaci, GiderPusulalari ikinci dokunuş — **gerekçe:** aynı paketin devir turu (kıyas devri + son doğrulama bulguları), başka dalga değil.

## I. DoD sözleşmesi

Yeni varlık/kayıt türü YOK; yeni uç, PATCH/DELETE, denetim kaydı gerektiren yol yok. Kaydet()/POST yolları, yetki ve yasal kapılar DEĞİŞMEDİ. Yeni form yok.
Yeni düğme yok (KarZarar Secici mevcut seçimin yerine; tıklama → istek, test M49 ile kilitli). Mevcut DOM id'leri korundu (`kz-karsilastir`, `sayfa-basi-*`, `vz-*`, `k-siz`, `panel-is-git-*`); yeni id'ler `panel-is-ayrinti-*`, `panel-is-cari-*`, `vz-kilit-*`.

## J. Koşul karşılama (29.09)

| Koşul | Durum | Kanıt |
|---|---|---|
| (A) başlık tek satır, ad hücresi ≤2 satır, sayfa yatay 0 | ✓ | tur A 0; C-1; yatay 0 |
| (B) para kartları sığar | ✓ | tur B 0; C-2; MUT-A 29 (taşan 1 geri) |
| (C) filtre yuvası hizalı | ✓ | tur C 0; /urunler 375 görüntü |
| (D) çekmece zıplatmaz | ✓ | sayfa-kaydı 0; MUT-B 27.09 (dosyalar değişmedi) |
| (E) Kim borçlu adı kesilmez | ✓ | tur E 0; 1440 görüntü |
| (F) "Bu reyonda mal yok." kırılmaz | ✓ | tur F 0; F1 |
| (G) BASİT jargon (uyum risk, şubeler, **vezne**) | ✓ | tur K8 ürün metni 0; G5/G7; vezne 1280 görüntü |
| (H) Yeniden dene ⟂ not | ✓ | H 0; H1 |
| Kıyas devri ORTAK-1 satırları | ✓ (has birimi hariç, gerekçeli) | § B |
| Test kiracısı (dahil 'PAKETORTAK-1-TEST Dogrulama') silindi | ✓ | § F sorguları 0 |
| Build 0 hata · paket testleri yeşil · mutasyon · denetim TEMİZ · yeni kırmızı 0 | ✓ | § E |
| Yeni bulgu yok | ✓ (alanımdaki yeniler düzeltildi; kalanlar sahipli/gerekçeli) | § D tablo |
| 1440 tuval karşılaştırması + 1280/375, BASİT/PRO, açık/koyu | ✓ | § D |

## K. Kurucuya / mali müşavire not

Mevzuat hükmü içeren değişiklik YOK.

---

# GEÇMİŞ — 27–28.09 devir turu raporu (aynen)

## 0. Devir turunda ne yapıldı (özet)

1. 25.09 raporu okundu; ağaçtaki iş **korundu, yeniden yazılmadı**. Tüm paket dosyaları ağaçta (§ 11); yarım mutasyon kalıntısı YOK
   (`// MUT` 0; `href="css/ortak.css"` 1; `RiskMetin => Dil.BasitMi` 1; `SubePasifeAlMetni => Dil.BasitMi` 1; Cekmece `birak` 1; `durum-notu-blok` 1).
   /tmp silindiği için 25.09 yedekleri yoktu → filtreli testler koşuldu: **OrtakGorunum 23/23 yeşil** (devir anı).
2. Kabul ölçümleri **güncel ağaçta** (25.09 18:05 `aksam-1810` yayınına giren tüm paketler + 27.09 şeritlerinin o anki işleri) yeniden koşuldu:
   ağacın Release anlık görüntüsü (`dotnet publish -c Release -o /private/tmp/kuyumcu-serit/ORTAK-1/yayin` → sonra `yayin2`), 5168'de, kendi kiracımla.
3. Ölçüm ve ekran görüntüsü incelemesi **6 yeni kusur** çıkardı (tur analizinin bir kısmını yakalamadığı, görüntüde görülen); hepsi paketin
   dosya alanında, ortak.css + 1 sayfa ile düzeltildi, test + mutasyonla kilitlendi (§ 2). Başka şeridin dosyasına dokunulmadı.

## 1. Kabul ölçümü — güncel ağaç (tur.mjs başsız Chrome, CDP 9601–9612, vekil 5169)

22 rota × {BASİT, PRO} × {açık, koyu} × {1440, 1280, 1024, 960, 768, 375} = **24 kombinasyon, 528 sayfa + 396 sekme + ~2 700 katman**, hata 0, geçersiz 0.

| Kategori | Bulgu | 25.09 ÖNCE (durum) | 25.09 SONRA | **27.09 SON (güncel ağaç)** |
|---|---|---|---|---|
| A | tablo başlığı / ad hücresi 2 satıra kırılıyor | 196 | 0 | **0** |
| B | para kartı değeri taşıyor | 77 | 0 | **0** |
| C | 375/960 filtre satırı hizasız | 8 | 0 | **0** |
| D | çekmece açılınca sayfa zıplıyor (açılış adedi) | 98 | 0 | **0** |
| E | Kim borçlu adı kesik | 9 | 0 | **0** |
| F | "Bu reyonda mal yok." kırık | 0* | 0 | **0** |
| G | BASİT jargon (uyum risk, şubeler, vezne) | 24 | 0 | **0** (/vezne dahil) |
| H | Yeniden dene ⟂ otomatik deneme notu | 0* | 0 | **0** |
| — | sayfa yatay kaydırma (K1) | 0 | 0 | **0** |

Kaynak: `ham/ORTAK-1-tur/analiz-SON-27-tam.txt` (tam tur), `analiz-SONRA-27-ilk.txt` (devir anındaki ağaç, düzeltmelerden önce),
`analiz-SONRA2-27-filtre-rotalari.txt`, `analiz-SON4-27-panel-raporlar.txt`, `analiz-SON5-27-statkart.txt` (düzeltme sonrası tekrarlar),
`karsilastirma-SONRA-SON-27.txt` (A–H satır satır: kalan 0; tek "yeni" anahtar /raporlar `panel-liste-tutar` — § 2 D-5 ile kapatıldı, SON4/SON5'te 0).
\* 25.09'da canlıda yeniden üretilemeyen F ve H bUnit + mutasyonla kilitli (F1, H1).

**Rota × genişlik × mod — son ölçüm (tur-SON + tekrar turları; "–" = bulgu yok):**

| Rota | BASİT (açık/koyu) 1440·1280·1024·960·768·375 | PRO (açık/koyu) 1440·1280·1024·960·768·375 |
|---|---|---|
| `/` (Panel) | – (pozisyon başlığı 960 **düzeltildi**, kasa alt satırı 960 **düzeltildi**) | 768: K10 `span.secici-metin` "Altınkaynak (varsayılan)" (KATMAN, Secici*) |
| `/raporlar` | 375: K4 tren 2.2× (ZO-BELGE/CUMA-J) | 375: K4 tren 2.2×; ?sekme=stok 375 K4 2.2× |
| `/urunler` | – (375 "Süzgeç" düğmesi **düzeltildi**) | 768–1440: K10 "Tüm kategoriler" (KATMAN) |
| `/isletme` | K8 "PAKET-ORTAK" = test kiracısı adı (ürün metni değil) | – |
| `/siparisler` | – (no/ad hücresi, "Yenile" **düzeltildi**) | – |
| Diğer 17 rota (/bildirimler, /cariler, /evraklar, /faturalar, /gider, /gider-pusulalari, /kasalar, /kullanicilar, /raporlar/islem-defteri, /raporlar/kar-zarar, /raporlar/kasa, /raporlar/satis-analizi, /raporlar/vade-takibi, /urunler/agac, /urunler/etiketler, /uyum, /vezne) | – | – |
| Tüm rotalar 375 (+PRO 768) | SERBEST-YÜZEN katman: genel arama "Müşteri, ürün ya da fi…" (GlobalArama — KATMAN şeridi; 25.09 SONRA'da yoktu, sonradan gelen KATMAN işi) | aynı |

**Ekran görüntüleri (Read ile incelendi)** — `ham/ORTAK-1-ekran/guncel-27/` (21 dosya): kok BASİT açık 960 (önce/SON), kok BASİT koyu 375,
raporlar_vade_takibi BASİT 960, siparisler BASİT koyu 960 (önce/SON), urunler BASİT 375 (önce/SON + koyu düzeltme), faturalar 375 (önce/SON),
kasalar BASİT 960, gider_pusulalari BASİT 1024, raporlar_kar_zarar 1024, uyum 1280, isletme koyu 1280, bildirimler 960, raporlar BASİT 1440 SON,
raporlar_satis_analizi PRO koyu 960 (SON/SON5), urunler PRO 1280, siparisler PRO 375.

**Tuval karşılaştırması (Panel, 1440×900, BASİT açık):** `ham/TUVAL-KARSILASTIRMA/ORTAK-1-Panel-1440-guncel-27.png` (tuval | 27.09 son görüntü).

| Bölge | Tuvalde | Bizde | Durum |
|---|---|---|---|
| Üst şerit, kenar menü, karşılama, "Yeni satış" | — | ORTAK-1 dokunmadı | AYNI (başka şeritlerin alanı) |
| 3 kart değer boyu | 36 px | 36 px (min() özgün boyu seçer) | AYNI |
| Kasa kartı alt satırı | "Nakit … Banka …" tek satır | "Nakit 7.841.802,73 ₺" / "Dolar kasalar 125.000,00 $" iki satır | GEREKÇELİ FARK — tuval verisinde döviz kasası yok; öğeler satıra sığmayınca sarar (taşma yok) |
| Pozisyon kartı başlığı | "Altın pozisyonum" | aynı + sağda "Ayrıntı" (1440'ta aynı satır) | AYNI |
| Kim borçlu ızgarası | ad · durum · tutar · eylem | aynı | AYNI |
| Uzun ad (26 karakter) | tuval verisinde yok | iki satıra sarar (üç nokta yok) | GEREKÇELİ FARK — (E) koşulu |
| Bekleyen iş | 4 satır | 5 satır + "İlk 5 iş gösterildi" bandı | GEREKÇELİ FARK — gerçek veri, UX-S29 sınır bandı (ORTAK-1 dışı) |

## 2. Devir turunda bulunan ve düzeltilen kusurlar (ölçüm/görüntüden)

| # | Kusur (nerede, nasıl ölçüldü) | Düzeltme | Test | Mutasyon |
|---|---|---|---|---|
| D-1 | /urunler 375 BASİT "Süzgeç ▾" ve /siparisler "Yenile" yazısı 44 px kutunun DİBİNE yapışık (görüntü; prob: düğmede `flex-direction:column; justify-content:flex-end` — app.css `.sayfa-basi-filtre > *` kuralı düğmeye de uyuyor). 25.09 ÖNCE görüntüsünde de vardı. | ortak.css: `.sayfa-basi .sayfa-basi-filtre > :is(button,.btn,.dugme) { flex-direction:row; justify-content:center; align-items:center }` | O4b | M-26 → O4b kırmızı |
| D-2 | /siparisler 960: sipariş no "S-2026-000003" 2 satır; "Karadeniz Kuyumculuk ve Mücevherat Ltd. Şti." 4 satır (A koşulu: ad hücresi ≤2 satır) | Siparisler.razor: no hücresine mevcut `hucre-tek-satir`, müşteri hücresine `hucre-ad`; ortak.css `.tablo-sar .hucre-ad { min-width:14em }` (id'ler aynı) | A1, A2 | M-27 → A1; M-28 → A1; M-29 → A2 |
| D-3 | /raporlar 1440 BASİT: `span.panel-liste-tutar "7.681.959,97 ₺"` taşıyor (tur SON'da YENİ anahtar — ZO-BELGE'nin 22:57 kasa listesi; app.css `--is` ızgarası `auto | minmax(0,1fr)` uzun kasa adına 'auto' veriyordu) | ortak.css `.panel-liste-satir--is:has(> .panel-liste-ad + .panel-liste-tutar) { grid-template-columns: minmax(0,1fr) auto }` + tutar nowrap (Raporlar.razor'a DOKUNULMADI) | B4 | M-30 → B4 |
| D-4 | Panel 960: `#pozisyon-baslik "Altın pozisyonum"` kırık (25.09 açık kalanı, dosyası AltinPozisyonuKarti bu paketin alanı) | ortak.css: başlık nowrap, başlık satırı `flex-wrap:wrap` ("Ayrıntı" dar kartta alt satıra) | B5 | M-31 → B5 |
| D-5 | Panel 960: kasa kartı alt satırı "Dolar kasalar 125.000,00 $" kart kenarını 6 px aşıyor (prob: öğe 185 px, kap 157 px; tuval kuralı öğeyi nowrap kilitliyordu) | ortak.css `.panel-sade .panel-soru .panel-soru-alt > * { white-space:normal; min-width:0 }` | B6 | M-32 → B6 |
| D-6 | /raporlar/satis-analizi PRO 960: "Giriş: 7.876.185,49 ₺" (en-uzun kademe, oran 6.5) ~10 px'e iniyordu — sığıyor ama okunmuyor (görüntü) | ortak.css: StatKart'ta cok-uzun/en-uzun kademe `clamp(1rem, oran×1cqi, 1.5rem)` + boşluktan sarma (tutar bölünmez) | B7 | M-33 → B7 |

Her düzeltmeden sonra ilgili rotalar 24 kombinasyonda yeniden turlandı (SONRA2, SON4, SON5): (A)–(H) 0, **yeni bulgu 0**.

## 3. Yapılanlar (dosya dosya — 25.09 + devir turu)

| Dosya | Değişiklik |
|---|---|
| `Kuyumcu.Web/wwwroot/css/ortak.css` (YENİ) | 25.09: (A) th tek satır + ilk sütun ad; (B) kap sorgusu + kademe oranı; (C) ≤640 tek sütun + düğme üst payı; (D) `body.pencere-sabit`; (E) Kim borçlu iki kat; (H) `.durum-notu-blok`. **27.09 ek:** D-1 … D-6 (yukarıda). Sabit renk yok, overflow:hidden/ellipsis yok. |
| `Kuyumcu.Web/wwwroot/index.html` | ortak.css EN SONDA (O1). |
| `Kuyumcu.Web/wwwroot/js/cekmece.js` | (D) sabitleme, preventScroll, bayat sayaç onarımı, `birak`. |
| `Kuyumcu.Web/Bilesenler/Cekmece.razor` | (D) OnParametersSet eşzamanlı sabitleme; IAsyncDisposable. id'ler AYNI. |
| `Kuyumcu.Web/Bilesenler/DegerBoyu.cs` (YENİ), `StatKart.razor`, `AltinPozisyonuKarti.razor`, `Pages/Panel.razor` | (B) `data-deger-boy` kademesi. |
| `Pages/Uyum.razor`, `Pages/Isletme.razor` | (G) BASİT metinleri OrtakGorunumSozlugu'ndan; PRO aynen. |
| `Pages/UrunAgaci.razor`, `Pages/Kullanicilar.razor`, `Pages/GiderPusulalari.razor` | (F), (H), stopaj tek satır sınıfları. |
| **`Pages/Siparisler.razor` (27.09)** | D-2: iki `<td>`'ye sınıf (no: `hucre-tek-satir`, müşteri: `hucre-ad`). Başka değişiklik yok; id'ler aynı. |
| `Kuyumcu.Core/I18n/Sozlukler.OrtakGorunum.cs` (YENİ) | TR+EN basit metinler. HUB'a dokunulmadı. |
| `Kuyumcu.Tests/OrtakGorunumTestleri.cs` (YENİ) | 23 → **30 test** (27.09 ek: O4b, A1, A2, B4, B5, B6, B7). |

**Dokunulan sayfalar (tek dokunuş listesi):** Panel.razor, Uyum.razor, Isletme.razor, UrunAgaci.razor, Kullanicilar.razor, GiderPusulalari.razor (25.09) +
**Siparisler.razor (27.09, ilk dokunuş — bu paketin dosya alanında: "Pages/Siparis*")**.
**Dokunulmayan:** app.css, Sozlukler.cs HUB, Raporlar.razor (ZO-BELGE), Kasalar, Cariler, Faturalar, Islemler, GlobalArama, Secici* — bunlardaki bulgular yalnız ortak.css ile çözüldü ya da sahibine devredildi.
**Kaydet()/POST yolları, yetki, yasal kapılar: DEĞİŞMEDİ.** Backend değişikliği YOK · **migration GEREKMEDİ.**

## 4. Sapmalar ve gerekçeleri (Anayasa md.1)

1. **Ölçüm hedefi:** 5168'de ağacın Release anlık görüntüsü koşuldu (başka şeritlerin eşzamanlı Debug derlemeleri ölçüme karışmasın). CSS-yalnız düzeltmelerde (D-1, D-3…D-6)
   anlık görüntüye yalnız `css/ortak.css` kopyalandı (ağaçtaki dosyayla `cmp` birebir); D-2 (razor) için anlık görüntü yeniden yayınlandı (`yayin2`).
2. **ÖNCE ölçümü yeniden koşulamadı:** 25.09 ÖNCE ham verisi /tmp ile silindi; ağaç geri alınamaz (git checkout yasak). Karşılaştırma: 25.09 ÖNCE/SONRA analiz metinleri + orkestratör tabanı
   (TAM-TUR-v3) + 27.09 devir anı turu (SONRA-27-ilk) ↔ son tur. (A)–(H) her aşamada 0.
3. **MASAK kuralı tohumda:** 185.000 TL ve üstü veresiye kimlik tespiti istedi (400) ve bölünmüş tutarlar kümülatif yakalandı (37× 400) — ürün kuralı doğru çalışıyor;
   gerçek kimlik verisi GİRİLMEDİ. Bu yüzden "Kim borçlu" en büyük tutar 179.999,94 ₺ (6 hane); 7 haneli tutarlar kasa (7.681.959,97 ₺ / 7.841.802,73 ₺) ve satış analizinden geldi.
   Eksi 7 haneli kasa üretilemez (kasa eksiye düşemez — sunucu kuralı, 400). Gider pusulası tohumlanmadı (hurda alış akışı) → /gider-pusulalari boş durumuyla ölçüldü; P1 bUnit kapsıyor.
4. **Mutasyon derleme yarışları:** aynı ağaçta 4–7 şerit eşzamanlı derliyor. İlk toplu koşumda M-1 ve M-4 "Kuyumcu.Tests.dll bulunamadı" ile GEÇERSİZ koştu; M-9 SATIS3EkranTestleri
   derleme hatası (başka şerit) yüzünden 6 denemede derlenemedi ve GEÇERSİZ sayıldı; M-27 ilk koşumda yarış yüzünden bayat ikiliyle "hayatta kaldı". Hepsi tekrarlandı (mut2.sh:
   derleme başarılı + test özeti var olana dek 6 deneme) → tabloda son, GEÇERLİ sonuçlar var. Bir ara derleme de B5'i "güncel" sanıp atladı (zaman damgası yarışı) → `touch` ile zorlandı.
5. **Tam süit ortam gürültüsü:** 23:44 koşumu 959 kırmızı verdi (başka şeridin derlemesi bin/ içindeki derlemeleri koşum ortasında sildi: `Could not load … Microsoft.IdentityModel.Tokens`,
   `Kuyumcu.Api.deps.json yok`); `--artifacts-path` ile yalıtılmış koşum kaynak ağacını bulamayan testler yüzünden geçersiz (202× "Kuyumcu.sln bulunamadı"). İkisi de GEÇERSİZ sayıldı;
   geçerli koşum § 5'te (ortam hatası sayacı 0).
6. **/vezne "Vezne" jargonu:** 27.09 turunda BASİT'te 0 durum (menü ve başlık "Kasa kuyruğu"); tabandaki bulgu yeniden üretilemedi. Kod değişikliği gerekmedi.
7. **(H) /kullanicilar:** bandın sebebi 25.09'da ölçüldü (yalnız 429 oran sınırı otomatik denemeyi açar). 27.09 turunda /kullanicilar 429 almadı; binme 0.

## 5. Testler

- Yeni test: **30** (`OrtakGorunumTestleri.cs`; 25.09'da 23, devir turunda +7). Filtreli son koşum: **OrtakGorunum 30/30**; ilgili mevcutlar
  (OrtakGorunum + KabukBilesen [SayfaBasi+Cekmece] + P36c + S31PanelSade + SiparisEkran) **125/125**; AltinPozisyonu dahil 129/129 (ara koşum).
- `dotnet build Kuyumcu.sln`: **0 hata, 1 uyarı** — uyarı `EtiketEkranTestleri.cs(145,47) xUnit1031` (24.09 tarihli dosya, bu paketin değişikliği değil; ORTAK-1 dosyalarından uyarı 0).
- **Tam süit (geçerli koşum, 28.09 00:3x, KUYUMCU_TEST_DB=kuyumcu_test_t6, trx /private/tmp/kuyumcu-serit/trx-pORTAK-1): 6419 test · 6413 geçti · 6 başarısız** — hiçbiri bu paketin değil:
  - `ZOBELGESinirTestleri.ZOBS05/ZOBS12/ZOBS13` — NullReferenceException `Raporlar.razor:1313` (BugunGunSonuKarti) → **ZO-BELGE** (Raporlar.razor 22:57'de değişti).
  - `S24EskiAltinEkranTestleri` ×2, `SATIS3EkranTestleri.Fare_ile_secim_de_ayni_kapidan_ekler` → `Pages.Islemler` → **SATIS-3**.
  - Önceki geçerli koşumda (23:01, 6373 test) 7 kırmızı vardı: P39 K5 + denetim TABLO_SARMALAYICISIZ (PosTahsilatlar → **ZO-ISLEM-A**, sonra düzeldi), S24 ×2 (SATIS-3),
    ve yük altında zaman aşımı veren S23E_01, CUMA_S_01, S25_E8, **OrtakGorunum H1** — dördü filtreli tekrarda yeşil (kararsızlık: 14.8 dk'lık koşum, tur ile eşzamanlı); son koşumda H1 yeşil.
  - **Paket ölçüsü (7):** bu paketin yeni kırmızısı **0** (tam süitteki 6 kırmızının hiçbiri ORTAK-1 dosyasına dokunan test değil).

## 6. Mutasyon tablosu

25.09 tablosu (M-ORTAK1-1 … 25) aynen geçerli; devir turunda **güncel ağaçta yeniden koşulanlar + yeniler** (yedek `/private/tmp/kuyumcu-serit/ORTAK-1/mut-yedek/`, her biri `cmp` birebir geri):

| Kod | Değişiklik | Kırmızıya dönen test | Geri alma |
|---|---|---|---|
| M-ORTAK1-1 | index.html ortak.css bağlantısı silindi | O1_index_ortak_cssi_EN_SONDA_yukler | cmp birebir (1. koşum GEÇERSİZ: dll yok → tekrar) |
| M-ORTAK1-4 | Uyum RiskMetin daima PRO | G1_uyum_risk_BASIT_musteri_der_PRO_cari_der | cmp birebir (1. koşum GEÇERSİZ → tekrar) |
| M-ORTAK1-5 | Isletme SubePasifeAlMetni daima PRO | G3_isletme_subeler_BASIT_kullanimdan_kaldir_ve_onay_ayni_ad | cmp birebir |
| M-ORTAK1-6 | Kullanicilar durum-notu-blok silindi | H1_oran_sinirinda_otomatik_deneme_notu_blok_satirdir | cmp birebir |
| M-ORTAK1-9 | th nowrap → normal | O2_tablo_basligi_tek_satir_ve_ad_hucresi_kirilmaz | cmp birebir (ilk koşum SATIS3 derleme hatası → GEÇERSİZ → tekrar) |
| M-ORTAK1-16 | Cekmece OnParametersSet sabitleme silindi | C1_acilista_sabitle_ac_tan_ONCE_bir_kez_cagrilir | cmp birebir |
| M-ORTAK1-22 | Cekmece Dispose'ta birak silindi | C2_acikken_yok_edilen_cekmece_kilidi_birakir | cmp birebir |
| M-ORTAK1-26 | D-1 düğme flex-direction row → column | O4b_filtre_seridindeki_dugme_yazisi_ortalanir_dibe_yapismaz | cmp birebir (aynı koşumda PN1/PN2 "SignalR.Client.Core yüklenemedi" = ortam gürültüsü) |
| M-ORTAK1-27 | Siparisler müşteri hücresinden hucre-ad silindi | A1_siparis_no_tek_satir_musteri_adi_hucresi_ad_sinifi_tasir | cmp birebir (1. koşum bayat ikili → hayatta kaldı → tekrar: kırmızı) |
| M-ORTAK1-28 | Siparisler no hücresinden hucre-tek-satir silindi | A1_… | cmp birebir |
| M-ORTAK1-29 | .hucre-ad min-width 14em → 0 | A2_ad_hucresi_en_az_genislik_kurali_ortak_cssde | cmp birebir |
| M-ORTAK1-30 | ad+tutar ızgarası `auto minmax(0,1fr)` (eski) | B4_ad_tutar_liste_satirinda_tutar_kendi_genisligini_alir | cmp birebir |
| M-ORTAK1-31 | pozisyon başlığı nowrap → normal | B5_pozisyon_basligi_bolunmez_ayrinti_alt_satira_iner | cmp birebir |
| M-ORTAK1-32 | kart alt satırı öğesi normal → nowrap | B6_panel_kart_alt_satiri_dar_kartta_tasmaz | cmp birebir |
| M-ORTAK1-33 | okunaklılık tabanı clamp → min (taban yok) | B7_uzun_kademe_stat_degeri_okunaklilik_tabanini_asmaz | cmp birebir |

Son mutasyondan sonra TEMİZ `dotnet build Kuyumcu.sln` → 0 hata; OrtakGorunum 30/30.

**TARAYICI MUTASYONU (KABUL koşulu, güncel anlık görüntüde):**
- **MUT-A — ortak.css bağlantısı yok** (anlık görüntü index.html; BASİT açık 1024+960, 9 rota): **(A) 26, (B) 13, (C) 1, (E) 2 durum GERİ GELDİ** (son turda 0).
  Çıktı: `ham/ORTAK-1-tur/analiz-MUT-A-ortakcss-yok-27.txt`. Geri: anlık görüntü index.html `cmp` birebir, sunucu yanıtında bağlantı 1.
- **MUT-B — çekmece kaydırma koruması yok** (`sabitle()` ilk satırı `return`; /cariler + /faturalar BASİT açık 768+375): **37 sayfa-kaydı açılışı GERİ GELDİ** (son turda 0).
  Çıktı: `ham/ORTAK-1-tur/analiz-MUT-B-kaydirma-korumasi-yok-27.txt`. Geri: cekmece.js `cmp` birebir.

## 7. Denetim

`bash denetim.sh` (son koşum, 28.09): **DENETİM TEMİZ.** Benim sayaçlarım ✓: record 3 (tavan 3) · app.css ham hex 25 (tavan 25) · satır içi style 90 (tavan 196) ·
ModalPencere 48 (tavan 55) · P-36c ODAK_TUZAGI 0 · PENCERE_KAYDIRMA_KILIDI 1 (tek yer, sayaçlı) · P-37 SAYFA_BASI_DISI_DUZEN 0 · KESIK_ETIKET 0.
Ara koşumda (23:5x) tek ✗ `P-39 TABLO_SARMALAYICISIZ 2` = PosTahsilatlar.razor (ZO-ISLEM-A, 22:45 değişikliği) — son koşumda giderilmişti.
25.09'daki `P-23 ODEME_KALEMSIZ_YOL 3` bugün yok.

## 8. Canlı doğrulama (5168 API anlık görüntüsü · 5169 vekil · başsız Chrome)

Kiracılar: `PAKET-ORTAK-1-TEST Kuyumculuk` (01M3J3231AE4HQRJ611SHS71RC) ve düzeltme sonrası tekrar ölçüm için `PAKET-ORTAK-1-TEST2 Kuyumculuk` (01M3JBHAVESAG0NKRG9F6A8R91).
Tohum yalnız ürünün uçlarından (demo-tohumla.js `--prova-kiraci` + ek betik): `POST /cari` 30×201, `POST /cari/{id}/veresiye` 24×200 + 58×400 (MASAK kuralı, § 4.3),
`POST /kasalar` 2×200 + `/hareket` 2×200, `POST /gider-kategorileri` 200, `POST /giderler` 2×200 + 1×400 ("kasa eksiye düşemez"), `POST /siparisler` 3×200. Yanıtlar okundu.
SQL (silmeden önce, kiracı 1): cariler 36 · siparisler 3 · kasalar 3; kiraci-temizle raporu 6 527 satır + tenants 1.
Prob (CDP, kendi sekmem): /urunler 375 BASİT `#urun-suzgec-ac` flex-direction column/flex-end → **row/center**; Panel 960 `#panel-soru-kasa-alt` scrollWidth 185 > clientWidth 157 → **157 = 157**.

## 9. DoD sözleşmesi

Yeni varlık/kayıt türü **YOK** (yalnız görünüm ve yerleşim). Yeni uç, PATCH/DELETE, denetim kaydı gerektiren yol yok. Siparisler.razor'da yalnız iki hücreye CSS sınıfı eklendi;
düğmeler, Kaydet/POST, yetki aynı. Yeni form yok. Mevcut DOM id'leri korundu (A1 `#sip-satir-s1` ile bulur; F1/G3/PN1 id ile bulur).

## 10. Test kiracısı ve süreçler

- 5168 API ve 5169 vekil durduruldu: `lsof` 5168 = 0, 5169 = 0; tur/prob Chrome süreçleri 0. 5199/5200'e dokunulmadı.
- Kiracı 1 silindi (`kiraci-temizle.sh … --uygula`: 6 527 satır + tenants 1) ve davet satırı (`kiraci_davetleri`, 1 satır). Kiracı 2 silindi (1 437 satır + tenants 1) ve davet satırı.
  Doğrulama: `tenants WHERE ad LIKE 'PAKET-ORTAK-1%'` = **0**, `kiraci_davetleri … 'PAKET-ORTAK-1%'` = **0**.
- tenants: başlangıç **15**, bitiş **17** — fark başka şeritlerin açık test kiracıları (PAKETSATIS-3-TEST, PAKETKATMAN-1b-TEST, PAKET-OLCEK-1E-TEST, PAKETEMANET-1-TEST — hepsi 27.09 20:01–21:03 açıldı). QA-TEST-* kiracılarına dokunulmadı.
- Sırlar (şifreleme anahtarı, bağlantı dizesi, JWT, sistem anahtarı) yalnız süreç ortamına `~/.kuyumcu/` ve chmod 600 dosyadan verildi; ekrana/rapora yazılmadı. Fikstür chmod 600.
- Geçici dosyalar yalnız `/private/tmp/kuyumcu-serit/ORTAK-1/` altında; proje ağacına .bak/.tmp bırakılmadı (ağaçtaki `Pages/Islemler.Sade.cs.tmp` bu paketin değil — SATIS-3 alanı).

## 11. Değişen dosyalar (`git status --porcelain | grep -v -E "/(bin|obj)/"`, bu paketinkiler)

```
 M dotnet/Kuyumcu.Web/Bilesenler/StatKart.razor
 M dotnet/Kuyumcu.Web/Pages/Kullanicilar.razor
 M dotnet/Kuyumcu.Web/Pages/Panel.razor
 M dotnet/Kuyumcu.Web/wwwroot/index.html
?? dotnet/Kuyumcu.Core/I18n/Sozlukler.OrtakGorunum.cs
?? dotnet/Kuyumcu.Web/Bilesenler/DegerBoyu.cs
?? dotnet/Kuyumcu.Web/wwwroot/css/ortak.css
?? dotnet/Kuyumcu.Tests/OrtakGorunumTestleri.cs
(izlenmeyen ağaçta, değişiklik bu pakette:) Bilesenler/AltinPozisyonuKarti.razor, Bilesenler/Cekmece.razor, Pages/GiderPusulalari.razor,
Pages/Isletme.razor, Pages/UrunAgaci.razor, Pages/Uyum.razor, Pages/Siparisler.razor (27.09: yalnız iki td sınıfı), wwwroot/js/cekmece.js
```
Ölçüm dosyaları: `ham/ORTAK-1-tur/*-27*.txt`, `ham/ORTAK-1-ekran/guncel-27/*`, `ham/TUVAL-KARSILASTIRMA/ORTAK-1-Panel-1440-guncel-27.png`.

## 12. Koşul karşılama

| Koşul | Durum | Kanıt |
|---|---|---|
| (A) tablo başlıkları tek satır, ad hücresi ≤2 satır, sayfa yatay kaydırma 0 | ✓ | A 0 (24 kombinasyon); D-2 A1/A2; K1 0 |
| (B) para kartları sığar ve okunur | ✓ | B 0; D-3/D-5/D-6; MUT-A B 13 geri |
| (C) 375 filtre yuvası hizalı, düğme yazısı ortalı | ✓ | C 0; D-1 O4b; prob row/center |
| (D) çekmece sayfa zıplatmaz | ✓ | sayfa-kaydı 0; MUT-B 37 geri |
| (E) Kim borçlu adı kesilmez | ✓ | E 0; MUT-A E 2 geri |
| (F) "Bu reyonda mal yok." kırılmaz | ✓ (bUnit+mutasyon) | F1, M-7 (25.09) |
| (G) BASİT jargon (uyum risk, şubeler, vezne) | ✓ | K8 ürün metni 0 (yalnız test kiracısı adı) |
| (H) Yeniden dene ⟂ not | ✓ | H1, M-6 |
| ortak.css ayrı, EN SONDA, app.css dokunulmadı | ✓ | O1, M-1; ham hex 25 |
| Mevcut id'ler korunur | ✓ | A1/F1/G3/PN1 id ile bulur |
| StatKart/SayfaBasi/Cekmece mevcut testleri yeşil | ✓ | KabukBilesen + ilgili 125/125 |
| Yeni bulgu yok | ✓ | karsilastirma-SONRA-SON-27 (tek yeni D-3 → kapatıldı, SON4/SON5 0) |
| 1440 tuval karşılaştırması + 1280/375, BASİT/PRO, açık/koyu | ✓ | § 1 |
| Mutasyon (ortak.css yok → A/B; kaydırma koruması yok → sayfa-kaydı) | ✓ | § 6 MUT-A / MUT-B |
| Build 0 hata · paketin yeni kırmızısı 0 · denetim TEMİZ · test kiracısı silindi | ✓ | § 5, § 7, § 10 |

## 13. Açık kalanlar (SAHİBİYLE)

1. Tam süit kırmızıları: `ZOBELGESinirTestleri.ZOBS05/12/13` (Raporlar.razor:1313 NRE) → **ZO-BELGE**; `S24EskiAltinEkranTestleri` ×2 + `SATIS3EkranTestleri.Fare_ile_secim…` → **SATIS-3**.
2. Genel arama katmanı 375 (ve PRO 768) "SERBEST-YÜZEN" — GlobalArama → **KATMAN** (25.09 SONRA'da yoktu).
3. K10 `span.secici-metin` kesik ("Altınkaynak (varsayılan)" PRO 768, "Tüm kategoriler" PRO 768–1440) → **KATMAN** (Secici*).
4. /raporlar ve /raporlar?sekme=stok 375 K4 sekme treni 2.2–2.3× → **ZO-BELGE / CUMA-J** (Raporlar*).
5. Derleme uyarısı `EtiketEkranTestleri.cs(145,47) xUnit1031` (24.09 dosyası) → sahibi UrunEtiket testinin yazarı (bu paket yazmadı; ORTAK-1 alanındaki sayfaya ait test — isterse orkestratör bu paketin sonraki turuna verebilir).
6. P-36c pencere sayacı ModalPencere'de de sızabilir (25.09 notu) — `sayaciDogrula` etkisini nötrlüyor; ModalPencere'ye Dispose önerilir → **KATMAN/Bileşen sahibi**.
7. Cariler BASİT çekmecesi Escape/arka plan tıklamasıyla kapanmıyordu (25.09 prob) → **CUMA-M**.
8. Eşzamanlı derlemeler bin/ içeriğini koşum ortasında siliyor (tam süit 959 sahte kırmızı) — orkestratör kapısı tam süiti şeritler durunca ölçmeli.

## 14. Kurucuya / mali müşavire not

Mevzuat hükmü içeren değişiklik YOK. MASAK 185.000 TL kimlik tespiti kapısı tohumlamada çalıştı (ürün kuralı; koda dokunulmadı).
