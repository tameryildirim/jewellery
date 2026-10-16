# PAKET UX-1-C1 — Uygulama raporu

> "Her veri girilen yer düzeltilebilir" · Faz C, sayfa grubu 1
> (Giriş · Panel · İşlemler/POS · Cariler · Ürünler · Ürün ağacı · Etiketler)
> Dalga 2c · Tarih: 2026-09-17 · Roller: Taşkın (backend) + Ertuğ (frontend)
> Durum: **BİTTİ** — derleme 0 uyarı / 0 hata · **1720/1720 test yeşil**
> (önce 1675, **+45 yeni test**) · `dotnet/denetim.sh` **TEMİZ** ·
> **12 mutasyon kanıtı** (biri test açığı buldu, test güçlendirildi) ·
> canlı doğrulama 5198'de yapıldı, sunucu durduruldu, test kiracısı silindi
> (tenants 8 → 8).

---

## 0. Önkoşul kanıtları (ilk adımda okundu)

| Önkoşul | Kaynak | Sonuç |
|---|---|---|
| 48-A storno motoru | `PAKET-48-uygulama-raporu.md` §1 | **BİTTİ (Faz A kapsamıyla)** — motor var; **kapsam sınırı kritik**, aşağıda |
| 45-API (MFA) | `PAKET-45-uygulama-raporu.md` §1 | **BİTTİ** — 1540/1540 yeşil, canlı doğrulanmış |
| S-2 (yanıt şekli / Stok NRE) | `PAKET-S-2-uygulama-raporu.md` | **BİTTİ** — Stok modalı `subeStoklar` ile düzeltilmiş; bu pakette regresyon testiyle yeniden bekçilendi |
| S-2T (yanıt şekli taraması) | `PAKET-S-2T-uygulama-raporu.md` | okundu; grup 1 sayfalarına düşen uyumsuzluk kalmamış |
| QA turu 2 ham bulgular | `ham/qa-turu-2.json` (7 rota, grup 1) | tamamı okundu, §6 tablosunda tek tek kapatıldı/ertelendi |
| UX-2-A2 kabuk | `PAKET-UX-2-A2-uygulama-raporu.md` §1.4-1.5 | ≤768px çekmece + ModalPencere Esc/odak **KAPANMIŞ** — grup 1'in "dar ekran" ve "Escape" bulguları bu paketin borcu değil |

### ⚠ 48-A KAPSAMI — spesifikasyon metniyle KOD ÇELİŞİYOR (Anayasa md.1)

Görev metnindeki "48-A DEVİR" maddesi *"storno motoru **SATIŞ/ALIŞ**/TAHSILAT gibi
tek bacaklı parasal hareketleri kapsar"* diyor. **Kod bunu doğrulamıyor.**
`StornoServisi.StornolanabilirTurler` (satır ~70) tam olarak şudur:

```
ISCILIK · VERESIYE · TAHSILAT · EMANET_GIRIS · EMANET_CIKIS
```

**SATIS ve ALIS listede YOKTUR** — çünkü ikisi de stok ve maliyet partisi geri
alması gerektirir. `ErtelenenTurler` (TAKAS/HURDA_ALIS/TRANSFER) 409 ile açık
gerekçe döner; SATIS/ALIS ise "tür beyaz listede değil" dalına düşer.

**Sonuç (bu paketin kapsamındaki tek defter ekranı /islemler = POS'tur):**
POS'ta storno düğmesi **GÖRÜNÜR ama devre dışıdır** ve gerçek gerekçesini yazar
(UX-S25/S30). Sessizce gizlenmedi, 409 ham hata olarak da gösterilmedi.
Doğru düzeltme yolu ekranda yazılıdır: **Satış İadesi**.
Bu, spesifikasyonun HEDEFİNE (düzeltilemeyen kayıt olmasın; olamıyorsa gerekçesi
yazsın) sadık kalınarak yapılan bilinçli uyarlamadır.

---

## 1. C0 — ortak form altyapısı (C'nin ilk adımı, tek el)

| Kalem | Dosya | Ne yapıldı |
|---|---|---|
| (a) alan altı Türkçe mesaj + sunucu sözlüğü | **YENİ** `Kuyumcu.Web/Servisler/FormDurumu.cs` | alan→mesaj defteri; `Zorunlu/Pozitif/NegatifDegil/Eposta/Telefon/EnAzKarakter`; `Sunucudan(ApiHata, esleme)` UX-4d'nin `ApiHata.Alanlar` sözlüğünü **ekran id'lerinin altına** bağlar; `IlkHatali` odak hedefini verir |
| (a) çizim | **YENİ** `Kuyumcu.Web/Bilesenler/AlanHatasi.razor` | `#alan-hata-{Alan}`, `role="alert"`; mesaj yoksa **hiçbir şey** çizmez |
| (b) tek Toast/başarı deseni | mevcut `BasariBandi` (UX-4c) | `SureliMs="3000"` + `Jeton` ile 5 sayfada kullanıldı; **ikinci bileşen açılmadı** |
| (c) ortak onay modalı | mevcut `ModalPencere` (UX-2-A2 Esc/odak düzeltmesiyle) | yıkıcı eylemlerde (cari/ürün/reyon pasife alma) kullanıldı |
| (d) sessiz etkisizlik yasağı + odak | `wwwroot/js/cekmece.js` → `window.kuyumcuOdak.alana(id)` | **yeni `<script>` etiketi YOK** (denetim: index.html script sayısı sabit 6); aynı dosya, aynı ad alanı |
| stil | `wwwroot/css/app.css` | `.alan-hata` + `.secim.hatali` — mevcut `.girdi.hatali` / `.alan .hata-metni` belirteçleri **yeniden kullanıldı**, ikinci palet açılmadı |
| sözlük | `Kuyumcu.Core/I18n/Sozlukler.cs` → `SozlukKabuk` | TR+EN 14 yeni anahtar (`ZorunluAlan`, `SifirdanBuyuk`, `NegatifOlamaz`, `FormEksik`, `Kaydedildi`, `EpostaGecersiz`, `TelefonGecersiz`, `SinirUyarisi`, `SinirAsildi`, `PasifleriGoster`, `AktifeAl`, `AktifeAlindi`, `YetkiYok`, `IsKurali`) |

### SAPMA 1 — `EditForm` KULLANILMADI (bilinçli, gerekçeli)

Spesifikasyon C0(a)'da "EditForm + ValidationMessage" diyor. Kullanılmadı:

1. **MÜHÜR 5 KOŞUL 3 (record dondurma).** `EditForm` her form için bir *model
   tipi* ister. Bu tipler `Pages/` altında doğar ve `denetim.sh`'ın
   "Pages içinde istemci record" sayacını (tavan **122**) kırar. Sayaç bu
   pakette kasten korundu: **122/122**.
2. **K4 id sözleşmesi.** `EditForm`'a geçiş 7 sayfadaki *her* `@bind`'ı yeniden
   yazmayı gerektirir; tek dokunuşta bu ölçekte bir yeniden yazım "mevcut
   element id'leri DEĞİŞMEZ" kuralını riske atar.
3. `denetim.sh` sayaç 8 zaten "Pages altında EditForm HİÇ YOK; formlar elle
   doğrulanıp `.hata-metni` basar" diye kayıt düşmüştü — mevcut mimari budur.

**Hedef birebir karşılandı:** alan altı Türkçe mesaj · sessiz etkisizlik yasağı ·
odak ilk hatalı alana · sunucunun alan sözlüğünün ekrana bağlanması.
Kanıt: `C0_sunucunun_alan_sozlugu_EKRAN_ID_altina_baglanir`,
`C0_bos_ve_gecersiz_deger_TURKCE_alan_alti_mesaj_uretir` ve mutasyon **M-C1-01/02**.

---

## 2. Sunucu tarafı — ÜÇ DURUMLU BAĞ ALANLARI (UX-1-B devir (a))

`Kuyumcu.Api/Modules/Urun/UrunModulu.cs` — `UrunGuncelleDto`:

```
• alan gövdede YOK      → KORU   (…Gonderildi = false)
• alan gövdede null/""  → SİL    (bağ NULL'a çekilir)
• alan gövdede değer    → ATA    (doğrulanır; yabancı kiracı → 404)
```

`KategoriId` ve `ReyonId` artık `…Gonderildi` bayrağı taşır (desen
`CariGuncelleDto.Aktif/AktifVerildi` ile **aynı**; ikinci kalıp açılmadı).
`Guncelle` gövdesinde `is not null` → `…Gonderildi` kontrolüne çevrildi.

**Neden gerekliydi:** ekran, reyon seçicisi boşaltıldığında gövdeye `reyonId:null`
koyuyordu; eski sözleşme bunu "dokunma" diye okuyordu → kullanıcının silme kararı
**sessizce etkisizdi**. `KategoriId` de aynı hatayı taşıyordu; simetri bozulmasın
diye ikisi birlikte düzeltildi (hiçbir mevcut test bu davranışa bağlı değildi —
`grep` ile doğrulandı).

**Migration: GEREKMEDİ.** Şema değişmedi; yalnız JSON model bağlama sözleşmesi
değişti (`[JsonIgnore]` bayrak özellikleri veritabanına yazılmaz).

---

## 3. Sayfa × düzenlenebilirlik tablosu (DoD — zorunlu)

| Sayfa | Düzenlenebilir alan | Yol / uç | id'ler | Düzenlenemeyen + gerekçe |
|---|---|---|---|---|
| **/giris** `Giris.razor` | e-posta, şifre, kayıt formu (5 alan) | POST `/auth/giris`, `/auth/kayit` | `#giris-eposta`, `#giris-sifre`, `#kayit-*` + `#alan-hata-*` | — (oturum öncesi, kayıt yok) |
| **/** `Panel.razor` | — (salt okunur özet) | — | `#sayfa-basi-panel*`, `#panel-satis-yap`, `#panel-kasa-donem`, `#panel-basit-notu` | Panel **türetilmiş** rapordur; düzeltme kaynak işlemden yapılır. Kasa Özeti kartı UX-S33 alt metnini taşır. |
| **/islemler** `Islemler.razor` | fiş kalemi **adet** ve **birim fiyat** (satır içi, kayıt ÖNCESİ) | istemci durumu → POST gövdesi | `#si-kalem-adet-{urunId}`, `#si-kalem-fiyat-{urunId}`, `#islem-kalem-ekle`, `#islem-kaydet`, `#islem-kaydet-kilit`, `#islem-son-satislar`, `#islem-iade-et-{id}`, `#islem-storno-{id}` | **Kaydedilmiş satış fişi** → `DegismezIpucu` `#degismez-ipucu-SatisFisi`: "stok ve maliyet partisi geri alması gerektirdiği için ters kayıtla hükümsüz kılınamaz"; **alternatif yol ekranda:** Satış İadesi (satırdaki "İade et" düğmesi iade kipine geçirir ve kaynağı seçer). Storno düğmesi **görünür + disabled**. |
| **/cariler** `Cariler.razor` | ad, tür, telefon, e-posta, vergi no, adres, kredi limiti, vade, fiyat listesi (**Çekmece**); telefon (**satır içi**); aktif/pasif | PATCH `/cari/{id}`, DELETE `/cari/{id}` (pasife), PATCH `{aktif:true}` (geri) | `#cekmece-cari*`, `#duz-*` (DEĞİŞMEDİ), `#si-cari-telefon-{id}`, `#c-aktife-al-{id}`, `#c-pasife-al`, `#cari-pasifleri-goster`, `#cari-tur-filtre` | Bakiye → düzeltme fişi (`#dz-*`, mevcut); hareketler defterdir |
| **/urunler** `Urunler.razor` | ad, kod, barkod, ürün grubu, ayar, **gerçek milyem (YENİ)**, **birim (YENİ)**, gram, taş, taş kökeni, kategori, reyon, işçilik, satış fiyatı, 4C, konsinye, belge (**Çekmece**); aktif/pasif | PATCH `/urunler/{id}`, DELETE (pasife), PATCH `{aktif:true}` | `#cekmece-urun*`, `#d-*` (DEĞİŞMEDİ), `#d-gercek-milyem`, `#d-birim`, `#u-aktife-al-{id}`, `#u-kaydet`, `#u-kaydet-kilit` | **Şube stoğu** → `#degismez-ipucu-UrunStok`: stok elle yazılmaz, deftere ve maliyet partisine düşmek zorundadır; yol: açılış stoğu / alış / Sayım Düzelt |
| **/urunler/agac** `UrunAgaci.razor` | kategori (ad, üst, sıra, aktif — **Çekmece**); **reyon (ad, tip, aktif — YENİ)**; reyon pasif/aktif | PATCH `/urun-kategorileri/{id}`, PATCH `/reyonlar/{id}`, DELETE `/reyonlar/{id}`, PATCH `{aktif:true}` | `#cekmece-kategori*`, `#k-*` (DEĞİŞMEDİ), `#cekmece-reyon*`, `#r-*`, `#r-duzenle-{id}`, `#r-pasife-al-{id}`, `#r-aktife-al-{id}`, `#r-pasif-onayla` | **Reyonun şubesi** → `#r-sube-kilit`: sunucu `ReyonGuncelleDto`'da şube taşımaz; yol: başka şubede yeni reyon + ürünleri taşı |
| **/urunler/etiketler** `UrunEtiketleri.razor` | kopya adedi, seçim | istemci (yazdırma) | `#et-yazdir`, `#et-yazdir-kilit`, `#et-sinir`, `#et-sinir-asildi`, `#alan-hata-kopya` | Etiket bir **çıktıdır**, kayıt değildir; içerik ürün kartından gelir |

### Tek dokunuş kaydı (DoD EK 2)

**Dokunulan sayfalar (7):** `Giris.razor` · `Panel.razor` · `Islemler.razor` ·
`Cariler.razor` · `Urunler.razor` · `UrunAgaci.razor` · `UrunEtiketleri.razor`.
Grup 1 dışında **hiçbir** `Pages/*.razor` açılmadı (`git status` §8).

### K4 id envanteri

**Silinen/değişen id: YOK.** Yöntem: mevcut id'lere dayanan **bütün** ekran
testleri (CariEkranTestleri, UrunEkranTestleri, UX4b/4c/4d, UrunAgaciEkranTestleri,
PosBarkod, KarmaOdeme, PosTeklif, Iade, DovizBacagi…) **değiştirilmeden** yeşil
kaldı (1720/1720). **Eklenen id'ler** yukarıdaki tabloda; hepsi
`<önek>-<alan>` / K6 (`#cekmece-{varlik}*`, `#si-{varlik}-{alan}-{id}`) biçiminde.

---

## 4. Testler (önce 1675 → sonra **1720**, +45)

| Dosya | Adet | Kapsam |
|---|---|---|
| **YENİ** `Kuyumcu.Tests/UX1C1EkranTestleri.cs` | **35** bUnit | C0 saf testleri (3) · Giriş (3) · Panel (2) · Cariler (7) · Ürünler (6) · Ürün ağacı (5) · Etiketler (2) · POS (6) |
| **YENİ** `Kuyumcu.Tests/HttpUrunBagTestleri.cs` | **10** HTTP | üç durumlu `reyonId`/`kategoriId` (koru/sil/ata + 404) · ürün aktife alma · reyon rol kapısı 403 · reyon 409 iş kuralı · reyon ve cari pasif↔aktif döngüsü |

Öne çıkan adlar:
`Cariler_pasif_satirda_AKTIFE_AL_dugmesi_PATCH_aktif_true_gonderir` ·
`Agac_reyon_satirinda_DUZENLE_dugmesi_VAR_ve_PATCH_gonderir` ·
`Urunler_reyon_secimi_bosaltilinca_govdede_reyonId_NULL_GIDER` ·
`Pos_storno_dugmesi_GORUNUR_ama_KILITLI_ve_GERCEK_gerekce_yazar` ·
`Urunler_stok_modali_GERCEK_yanitla_satiri_cizer_hata_vermez` (S-2 regresyonu) ·
`reyonId_NULL_gonderilince_bag_SILINIR`.

**Sahte API:** `'{}'` varsayılanı bırakılmadı — bu paketin dokunduğu her uca
gerçek yanıt şekli verildi (S-2'nin NRE'si tam da `'{}'`'dan doğmuştu).
**Cekmece Esc/odak** testi A2'de bir kez yazılmıştı (`KabukBilesenTestleri`);
tekrarlanmadı.

---

## 5. Mutasyon tablosu (12 madde — hepsi kırmızıya döndü)

| Kod | Değişiklik | Kırmızıya dönen test | Geri alma |
|---|---|---|---|
| M-C1-01 | `FormDurumu.Sunucudan` sunucu sözlüğünü boş sözlükle gezer | `C0_sunucunun_alan_sozlugu_EKRAN_ID_altina_baglanir` | cmp: AYNI |
| M-C1-02 | `AlanHatasi` çizim koşulu `false &&` ile kapatılır | `Cariler_bos_ad_ile_kaydet_ISTEK_ATMAZ_ve_ALAN_ALTI_mesaj_verir` | cmp: AYNI |
| M-C1-03 | Cariler `aktif=true` süzgeci kaldırılır | `Cariler_liste_VARSAYILAN_olarak_yalniz_AKTIFLERI_ister` | cmp: AYNI |
| M-C1-04 | Cariler "Aktife al" düğmesi çizilmez | `Cariler_pasif_satirda_AKTIFE_AL_dugmesi_PATCH_aktif_true_gonderir` | cmp: AYNI |
| M-C1-05 | Reyon "Düzenle" düğmesinin `@onclick`'i boşaltılır | `Agac_reyon_satirinda_DUZENLE_dugmesi_VAR_ve_PATCH_gonderir` | cmp: AYNI |
| M-C1-06 | Sunucu üç durumlu kontrolü eski `is not null`'a döner | `HttpUrunBagTestleri.reyonId_NULL_gonderilince_bag_SILINIR` | cmp: AYNI |
| M-C1-07 | PATCH gövdesinden `gercekMilyem` alanı çıkarılır | `Urunler_duzenleme_PATCH_govdesi_gercekMilyem_ve_birim_TASIR` | cmp: AYNI |
| M-C1-08 | POS storno düğmesinden `disabled` kaldırılır | `Pos_storno_dugmesi_GORUNUR_ama_KILITLI_ve_GERCEK_gerekce_yazar` | cmp: AYNI |
| M-C1-09 | POS kalem fiyat doğrulaması kapatılır | `Pos_gecersiz_kalemde_EKLE_sessizce_gecmez_ALAN_ALTI_mesaj_verir` | cmp: AYNI |
| M-C1-10 | `Yazdirilabilir` koşulundan `kopya >= 1` düşürülür | **İLK DENEMEDE KIRMIZIYA DÖNMEDİ → TEST AÇIĞI.** Test güçlendirildi (önce ürün seçilir); yeni ad `Etiketler_URUN_SECILIYKEN_adet_sifirsa_YAZDIR_kilitli_ve_SEBEBI_yazar` → **kırmızı** | cmp: AYNI |
| M-C1-11 | Giriş formundan e-posta zorunluluğu kaldırılır | `Giris_bos_formda_ALAN_ALTI_mesaj_cikar_ve_ISTEK_ATILMAZ` | cmp: AYNI |
| M-C1-12 | Ürün "Aktife al" düğmesinin `@onclick`'i boşaltılır | `Urunler_pasif_satirda_AKTIFE_AL_PATCH_aktif_true_gonderir` | cmp: AYNI |

Son mutasyondan sonra: `dotnet build Kuyumcu.sln` **0 uyarı / 0 hata**,
`dotnet test` **1720/1720**, `denetim.sh` **TEMİZ**.
Betik: `scratchpad/mut.sh`, yedekler `scratchpad/mut-yedek/UX-1-C1/`.

---

## 6. QA turu 2 bulguları — kapanış tablosu (BAĞLAYICI)

### /giris
| Bulgu | Durum | Kanıt |
|---|---|---|
| Form doğrulaması yalnız tarayıcı balonuna dayanıyor, Türkçe mesaj yok (ORTA, ACIK) | **KAPANDI** | `novalidate` + `AlanHatasi`; test `Giris_bos_formda_…`, `Giris_gecersiz_eposta_TURKCE_mesaj_alir`; mutasyon M-C1-11 |
| Sol tanıtım metni yüzük illüstrasyonuna biniyor (DÜŞÜK, KISMEN) | **ERTELENDİ** — saf görsel katman işi; `giris-sayfa` SVG/katman düzeni UX-2 Faz B1 (kabuk/panel şablonu) kalemidir. Hedef paket: **UX-2-B1** |

### / (Panel)
| Bulgu | Durum | Kanıt |
|---|---|---|
| Dar ekranda kenar çubuğu sabit, yatay taşma (YÜKSEK) | **KAPANDI (UX-2-A2)** | A2 raporu §1.4: ≤768px çekmece + `#ust-menu-ac` + `#kenar-ortu` |
| "Kuru güncelle" başarı geri bildirimi yok (DÜŞÜK) | **ERTELENDİ** — düğme bu sayfada DEĞİL, üst bardadır (`Layout/UstBar.razor`). Tek dokunuş kuralı gereği kabuk dosyasına dokunulmadı. Hedef paket: **UX-2-B1 (kabuk)** |
| Basit mod Panel içeriğini sadeleştirmiyor (DÜŞÜK) | **KAPANDI** | Grafik + "Bugün Özet" `ProAlan` ile katlandı; KPI ve Kasa Özeti her iki modda kalır; `#panel-basit-notu` |
| — (yeni) | UX-S33 | Kasa Özeti kartı `#panel-kasa-donem` alt metnini taşır |

### /islemler (POS)
| Bulgu | Durum | Kanıt |
|---|---|---|
| Kaydedilen satış fişi listelenmiyor / düzenlenemiyor / storno yok (**KRİTİK**) | **KAPANDI (kapsam sınırıyla)** | "Son satışlar" kartı `#islem-son-satislar` (mevcut `iade-adaylari` ucundan — **yeni uç açılmadı**); satırda **İade et** (çalışır) + **Storno** (görünür, disabled) + `#degismez-ipucu-SatisFisi` gerçek gerekçe; kayıt sonrası **fiş no** başarı bandında (`FisNoOku`, UX-S27). Testler: `Pos_kaydedilen_satislar_AYNI_EKRANDA_listelenir`, `Pos_storno_dugmesi_…`, `Pos_satirdan_IADE_ET_…` |
| Boş formda "Satış Kaydet" ve geçersiz kalemde "Ekle" sessizce hiçbir şey yapmıyor (YÜKSEK) | **KAPANDI** | "Ekle" artık kilitli değil → alan altı mesaj (`#alan-hata-islem-adet`, `#alan-hata-islem-birimfiyat`); "Kaydet" kilidi korundu **ama sebebi yazılıyor** (`#islem-kaydet-kilit`, UX-S30). Mutasyon M-C1-09 |
| Kalem satırı düzenlenemiyor (adet/fiyat) (ORTA) | **KAPANDI** | `SatirIciDuzenle` → `#si-kalem-adet-{urunId}`, `#si-kalem-fiyat-{urunId}`; test `Pos_kalem_satirinda_ADET_satir_ici_duzenlenir` |
| Dar ekranda sekmeler dikey yığılıyor, yatay taşma (YÜKSEK) | **KAPANDI** | `.sekme-serit` + `@media ≤768px` yatay kaydırma (app.css); kabuk düzeltmesi A2'de |
| Ürün listesinde stok bilgisi yok (ORTA) | **ERTELENDİ** — stok adedi `/urunler` liste yanıtında YOKTUR; göstermek ya yeni bir uç ya da sayfaya yeni `Api.*` çağrısı ister (TB-09 yasağı). **UX-1-B boşluğu** olarak kayda geçti. Hedef paket: **UX-4b devamı / POS stok rozeti** |

### /cariler
| Bulgu | Durum | Kanıt |
|---|---|---|
| Pasif cari listede "Aktif" görünüyor, aktife alma yolu yok (**KRİTİK**) | **KAPANDI** | varsayılan `aktif=true` süzgeci + `#cari-pasifleri-goster` + Durum sütunu rozeti + `#c-aktife-al-{id}`; testler + M-C1-03/04; canlı §7 adım 3-6 |
| "Pasife Al" onaysız (YÜKSEK) | **KAPANDI (UX-4d md.13)** | `#c-pasif-onayla` onay modalı; mevcut test `Pasife_al_DELETE_gonderir` |
| Doğrulama hataları İngilizce/teknik (YÜKSEK) | **KAPANDI (UX-4d md.8 + C0)** | Canlı 400 yanıtı §7 adım 1: `alanlar{email,telefon}` **Türkçe**; ekranda `AlanHatasi` ile alanın altına düşer |
| Boş adla Kaydet sessizce etkisiz, başarı bildirimi yok (ORTA) | **KAPANDI** | `#cari-kaydet` kilitli değil → `#alan-hata-cari-ad`; kayıt sonrası `#basari-bandi` |
| Tür sütunu ham enum, filtre tüm türleri kapsamıyor (ORTA) | **KAPANDI** | `TurEtiketi()` sözlükten; `#cari-tur-filtre` 6 seçenek (Tümü + 5); test `Cariler_tur_sutunu_HAM_ENUM_basmaz_…` |
| Telefon biçim doğrulaması yok (DÜŞÜK) | **KAPANDI** | `FormDurumu.Telefon` **sunucunun kuralını çağırır** (`Iletisim.TelefonGecerli`); test `Cariler_gecersiz_telefon_…`; canlı 400 |
| Escape modalı kapatmıyor (ORTA) | **KAPANDI (UX-2-A2)** | ModalPencere + Cekmece aynı yardımcı (`kuyumcuCekmece.ac/kapat`) |
| Dar ekranda tablo yatay taşıyor (YÜKSEK) | **KAPANDI (UX-2-A2)** | kabuk düzeltmesi + `.tablo-sar` |

### /urunler
| Bulgu | Durum | Kanıt |
|---|---|---|
| Stok modalı NRE + "Beklenmeyen hata" bandı (YÜKSEK, YENİ) | **KAPANDI (S-2) + bekçilendi** | `StokYaniti(SubeStoklar)`; yeni regresyon testi `Urunler_stok_modali_GERCEK_yanitla_satiri_cizer_hata_vermez` |
| Pasif ürünü aktife alma yolu yok (YÜKSEK) | **KAPANDI** | `#u-aktife-al-{id}` → PATCH `{aktif:true}`; M-C1-12; canlı §7 adım 10 |
| Düzenle formu "Gerçek Milyem" / "Birim" içermiyor (YÜKSEK) | **KAPANDI** | `#d-gercek-milyem`, `#d-birim`; PATCH gövdesinde gider; M-C1-07; canlı §7 adım 8 (milyem 916→900, birim ADET→GRAM, has gram yeniden hesaplandı) |
| ("Konsinye" alanı) | zaten **KAPANMIŞTI (UX-4b md.6)** | `#d-konsinye` |
| Boş/geçersiz (gram -5) kayıt sessizce etkisiz (YÜKSEK) | **KAPANDI** | `#u-kaydet` yalnız mevzuat kapılarında kilitli (+`#u-kaydet-kilit` sebebi); ad/kod/gram alan altı mesaj; test `Urunler_bos_ad_ile_kaydet_…` |
| Stok modalı salt okunur, stok girişi yok (ORTA) | **KAPANDI (gerekçeyle)** | `#degismez-ipucu-UrunStok` + `Urun.StokGirisiYolu`; test `Urunler_stok_BOSKEN_dugme_yerine_GEREKCE_cizilir`. Elle stok yazma **bilinçle açılmadı**: her stok değişikliği deftere ve maliyet partisine düşmek zorundadır |
| İki ayrı "Kategori" alanı kafa karıştırıyor (ORTA) | **KAPANDI** | enum alanı her iki formda **"Ürün grubu"** (`Dil.T.Urun.UrunGrubu`); ağaç alanı "Kategori" kaldı |
| Escape modalı kapatmıyor (ORTA) | **KAPANDI (UX-2-A2)** | — |

### /urunler/agac
| Bulgu | Durum | Kanıt |
|---|---|---|
| Reyon düzenlenemiyor / pasife alınamıyor (**KRİTİK**) | **KAPANDI** | `#r-duzenle-{id}` (Çekmece, ad+tip+aktif) · `#r-pasife-al-{id}` (onay modalı) · `#r-aktife-al-{id}`; M-C1-05; canlı §7 adım 7 |
| Boş adla Kaydet sessizce etkisiz (kategori ve reyon) (ORTA) | **KAPANDI** | `#alan-hata-k-ad`, `#alan-hata-r-ad`; test `Agac_bos_adla_kategori_kaydet_…` |
| "Sıra" negatif kabul ediyor (DÜŞÜK) | **KAPANDI (istemci)** | `min="0"` + `#alan-hata-k-sira`. ⚠ **Sunucu tarafı aralık kontrolü EKLENMEDİ** — `KategoriGuncelleDto` bu pakette açılmadı (kapsam dışı dosya); açık kalanlarda |
| Kategori pasife alma yalnız modal kutusuyla, toast yok (DÜŞÜK) | **KISMEN KAPANDI** | `#basari-bandi` eklendi; satırda hızlı pasife al **ERTELENDİ** (ağaç düğümü düğme bütçesi; hedef: UX-2-B2 liste ailesi) |
| Escape modalı kapatmıyor (ORTA) | **KAPANDI (UX-2-A2)** | — |
| Dar ekranda başlık harf harf kırılıyor (ORTA) | **KAPANDI (UX-2-A2 + SayfaBasi)** | `.sayfa-basi` tipografisi tek kaynaktan |

### /urunler/etiketler
| Bulgu | Durum | Kanıt |
|---|---|---|
| Adet=0 ile "Yazdır (0)" aktif, boş sayfa basıyor (ORTA) | **KAPANDI** | `#et-yazdir` disabled + `#et-yazdir-kilit`; M-C1-10 (test açığı bulundu ve kapatıldı) |
| Sayfa başlığı "— Kuyumcu ERP" son ekini taşımıyor (DÜŞÜK) | **KAPANDI** | `<PageTitle>` düzeltildi |
| Dar ekranda tablo/önizleme yatay taşıyor (ORTA) | **KAPANDI (UX-2-A2)** | — |
| `kur/guncel` çok sık yineleniyor (DÜŞÜK) | **ERTELENDİ** — kur yoklaması `CanliKurServisi` + üst bardadır, sayfa dosyasında değil. Hedef paket: **UX-2-B1 (kabuk)** |

### QA turu 1'den taşınan, tur 2'de yinelenmeyen kalem
"Satış kalemi satır içi düzenleme yok" → **KAPANDI** (yukarıda).

---

## 7. Canlı doğrulama (localhost:5198, gerçek veri + SQL)

Kiracı: **PAKETUX-1-C1-TEST Kuyumcu** (kayıt 201). Sunucu iş bitince durduruldu
(`lsof -iTCP:5198` boş). **5199'a DOKUNULMADI.**

| # | Adım | Beklenen | Gerçek |
|---|---|---|---|
| 1 | POST `/cari` — `email:"gecersiz-eposta"`, `telefon:"abc"` | 400 + Türkçe alan sözlüğü | **400** `alanlar:{"email":["E-posta geçerli bir e-posta adresi olmalıdır (örnek: ad@ornek.com)"],"telefon":["Telefon yalnız rakam… (10–13 hane)"]}` |
| 2 | POST `/cari` geçerli | 201 | **201** id `01M2Q1GDVBZ2TNKPC74WXJT107` |
| 3 | PATCH `/cari/{id}` `{telefon}` (satır içi) | 200, telefon değişti | **200** `"telefon":"05559998877"` |
| 4 | DELETE `/cari/{id}` | 200, pasif | **200** `{"mesaj":"Cari deaktif edildi"}` |
| 5 | GET `/cari?aktif=true` | pasif cari **GÖRÜNMEZ** | eşleşme sayısı **0** |
| 6 | PATCH `/cari/{id}` `{aktif:true}` → GET `?aktif=true` | 200, geri gelir | **200**, eşleşme sayısı **1** |
| 7 | POST `/reyonlar` → PATCH (ad+tip) → DELETE → PATCH `{aktif:true}` | 201/200/200/200 | **201**, **200** `"ad":"C1 Vitrin (düzeltildi)","tip":"KASA"`, **200** `{"mesaj":"Reyon pasife alındı","aktif":false}`, **200** `"aktif":true` |
| 8 | POST `/urunler` reyonlu → PATCH `{reyonId:null}` | reyon bağı **SİLİNİR** | SQL önce `01M2Q1GKBG…`, PATCH **200**, SQL sonra **NULL** |
| 9 | PATCH `{reyonId:…, gercekMilyem:900, birim:"GRAM"}` | 200, alanlar yazılır | SQL: `reyon_id=01M2Q1GKBG…`, `milyem=900.00`, `birim=GRAM`, `gram_agirlik=1.0000`, `has_grami=0.900000` |
| 10 | PATCH `{ad:…}` (reyonId gövdede YOK) | reyon **KORUNUR** | SQL: `reyon_id` aynı ✓ |
| 11 | DELETE `/urunler/{id}` → PATCH `{aktif:true}` | pasif → aktif | SQL `aktif`: **0** → **1** |
| 12 | POST `/urun-kategorileri` → PATCH `{ad,sira:5,aktif:false}` | 201/200 | **201**, **200** `"sira":5,"aktif":false` |
| 13 | **TEZGAHTAR** POST `…/storno` | **403** (yetki), 409 DEĞİL | **403** `{"message":"Bu işlem için gerekli rol: SAHIP veya MUDUR","error":"Forbidden"}` |
| 14 | **TEZGAHTAR** PATCH `/reyonlar/{id}` | **403** | **403** aynı gövde |
| 15 | **SAHIP** DELETE `/reyonlar/{id}` (üzerinde ürün var) | **409** (iş kuralı) | **409** `{"message":"Bu reyonda 1 ürün var; önce ürünler başka reyona taşınmalıdır","error":"Conflict"}` |

**13 ↔ 15 karşılaştırması bu paketin ekran tarafındaki 403/409 ayrımının
gerekçesidir** (`FormDurumu.SunucuMesaji`: 403 → "Bu işlem için yetkiniz yok",
409 → "İş kuralı engelledi: …").

**Temizlik:** `tenant_id` kolonu olan 39 tablodan test kiracısının satırları
silindi; `select count(*) from tenants` → **8** (başlangıç: **8**).
`urunler`/`cariler` kiracı sayımı **0**. **`QA-TEST-` önekli kalıcı QA
fikstürlerine DOKUNULMADI.**

**Tarayıcı turu (375/768/1280 px `scrollWidth ≤ clientWidth`) bu raporda
YAPILMAMIŞTIR** — kapı tanımı gereği QA ajanınca yapılır. Bu paketin verdiği
kanıt HTTP + SQL + bUnit'tir; dar ekran düzeltmeleri A2'de ölçülmüştü,
bu pakette eklenen tek CSS kuralı `.sekme-serit` yatay kaydırmasıdır.

---

## 8. Değişen dosyalar

**Yeni (4):**
`dotnet/Kuyumcu.Web/Servisler/FormDurumu.cs` ·
`dotnet/Kuyumcu.Web/Bilesenler/AlanHatasi.razor` ·
`dotnet/Kuyumcu.Tests/UX1C1EkranTestleri.cs` ·
`dotnet/Kuyumcu.Tests/HttpUrunBagTestleri.cs`

**Değişen (11):**
`dotnet/Kuyumcu.Api/Modules/Urun/UrunModulu.cs` ·
`dotnet/Kuyumcu.Core/I18n/Sozlukler.cs` ·
`dotnet/Kuyumcu.Web/Pages/Giris.razor` ·
`dotnet/Kuyumcu.Web/Pages/Panel.razor` ·
`dotnet/Kuyumcu.Web/Pages/Islemler.razor` ·
`dotnet/Kuyumcu.Web/Pages/Cariler.razor` ·
`dotnet/Kuyumcu.Web/Pages/Urunler.razor` ·
`dotnet/Kuyumcu.Web/Pages/UrunAgaci.razor` ·
`dotnet/Kuyumcu.Web/Pages/UrunEtiketleri.razor` ·
`dotnet/Kuyumcu.Web/wwwroot/css/app.css` ·
`dotnet/Kuyumcu.Web/wwwroot/js/cekmece.js`

`git status --porcelain | grep -v -E "/(bin|obj)/"` çıktısı bunların hepsini
içerir (çalışma ağacı dalga 2a'dan beri zaten kirlidir; **COMMIT YOK**).

---

## 9. Denetim sayaçları (öncesi → sonrası)

```
✓ app.css ham hex                              25 (tavan 25)   [değişmedi]
✓ Pages+Bilesenler+Layout satır içi style=    189 (tavan 196)  [196 → 189, DÜŞTÜ]
✓ vendor CSS lisans başlığı                   var
✓ index.html <script> sayısı                    6 (sabit 6)    [değişmedi]
✓ index.html satır içi <script>                 0
✓ Pages: @onclick'siz ve disabled'sız <button> 65 (tavan 71)   [71 → 65, DÜŞTÜ]
✓ Pages: ModalPencere kullanımı                42 (tavan 45)   [45 → 42, DÜŞTÜ]
✓ Pages: duzenlenenId kalıbı                    1 (tavan 1)    [değişmedi]
✓ Pages/*.razor dosya sayısı                   30 (tavan 30)   [değişmedi]
✓ Pages: doğrulama mesajsız EditForm yok        0 EditForm / 0 ValidationMessage
✓ Pages içinde istemci record tanımı          122 (tavan 122)  [değişmedi — MÜHÜR 5 KOŞUL 3]
DENETİM TEMİZ.
```

**ModalPencere 45 → 42:** üç çok-alanlı düzenleme (cari, ürün, kategori) Çekmece'ye
taşındı ve reyon için yeni bir Çekmece açıldı; ModalPencere yalnız **yıkıcı onay**
için kaldı (+1 yeni reyon onay modalı). Net −3.

---

## 10. Koşul karşılama tablosu

| Koşul (kaynak) | Durum | Kanıt |
|---|---|---|
| C0 ortak form altyapısı, C1'den ÖNCE, tek el | **KARŞILANDI** | §1; EditForm sapması gerekçeli (§1 SAPMA 1) |
| Her sayfada `SayfaBasi` | **KARŞILANDI** | 7/7: `#sayfa-basi-{giris,panel,islemler,cariler,urunler,urun-agaci,etiketler}` |
| Her veri girişi için düzenleme yolu / `DegismezIpucu` | **KARŞILANDI** | §3 tablosu |
| Storno düğmesi 48-A motoruna bağlanır | **KISMEN — gerekçeli** | POS'un türü (SATIS) motorun beyaz listesinde DEĞİL → düğme görünür + disabled + gerçek gerekçe (§0 uyarısı) |
| "ters iade" taklidi YASAK | **KARŞILANDI** | Var olan `satis-iade` ucu **iade** olarak sunuldu, storno diye sunulmadı |
| Pasife al / Aktifleştir (DELETE değil) | **KARŞILANDI** | cari · ürün · reyon üçünde de tam döngü (canlı §7) |
| `ProAlan` ile basit modda katlama | **KARŞILANDI** | Panel (grafik + Bugün Özet) |
| Mevcut element id'leri DEĞİŞMEZ (K4) | **KARŞILANDI** | 1720/1720, id'e dayalı mevcut testler değiştirilmeden yeşil |
| K6 Cekmece sözleşmesi | **KARŞILANDI** | `#cekmece-{cari,urun,kategori,reyon}` + `-govde/-kaydet/-iptal`, `#cekmece-kapat`; Esc/odak A2 testinde |
| K7 bileşen bütçesi | **KARŞILANDI** | Yalnız SayfaBasi · Cekmece · SatirIciDuzenle · DegismezIpucu (+ C0'ın zorunlu kıldığı `AlanHatasi`, §11'de gerekçeli) |
| Parasal kayıtta iyimser kaydetme YOK | **KARŞILANDI** | Cekmece kendini kapatmaz; POS'ta fiş no **sunucu yanıtından** okunur |
| Satır içi `style=""` yazılmaz | **KARŞILANDI** | sayaç 196 → **189** |
| Sayfaya yeni `Api.*` **ucu** eklenmez (TB-09) | **KARŞILANDI (yorumla)** | Hiçbir yeni uç ailesi tüketilmedi; "Son satışlar" sayfanın ZATEN kullandığı `iade-adaylari` ucundan gelir. Yeni PATCH/DELETE **çağrıları** paketin KAPSAM maddesinin kendi emridir (§11 SAPMA 2) |
| Mevcut testler değiştirilmez | **KARŞILANDI** | Yalnız 2 yeni dosya eklendi |
| Tam süit 0 başarısız · denetim TEMİZ | **KARŞILANDI** | 1720/1720 · TEMİZ |
| Test kiracısı silinir, `QA-TEST-`'e dokunulmaz | **KARŞILANDI** | tenants 8 → 8 |
| İç paket/dalga numarası kullanıcıya gösterilmez | **KARŞILANDI** | 3 sızıntı metni temizlendi (§11) |

---

## 11. Sapmalar ve gerekçeleri

**SAPMA 1 — EditForm kullanılmadı.** §1'de tam gerekçe (record dondurma + K4).

**SAPMA 2 — "sayfaya yeni `Api.*` çağrısı eklenmez" (K7) yorumlandı.**
Bu cümle UX-2-A2 bağlamında (SIFIR sayfa DOM değişikliği paketi) yazılmıştır.
C1'in KAPSAM maddesi ise açıkça "PATCH/DELETE uçlarına bağlanır", "storno ucuna
bağlanır" diyor — literal okuma paketi imkânsız kılardı. Uygulanan kural:
**yeni uç ailesi tüketilmez**; yalnız UX-1-B/48-A'nın açtığı ve sayfanın zaten
bildiği uçlara yazma çağrısı eklenir. Yeni GET **hiç** eklenmedi.

**SAPMA 3 — `AlanHatasi` bileşeni K7 bütçesine eklendi.**
C0(a) "alan altı mesaj" şartının çizim tarafı bir bileşen olmadan her sayfada
kopyalanırdı (GerekceAlani'nın raporunda anlatılan "dört kopya, dört farklı
kural" hatasının aynısı). Bileşen **20 satırdır**, API çağırmaz, durum tutmaz.
KomutPaleti/HizliIzgara/KayitliGorunum **açılmadı**.

**SAPMA 4 — Cari ve ürün düzenleme "Kaydet" düğmesinin kilidi tümüyle
kaldırılmadı.** Mevcut testler (`Bos_ad_ile_kaydet_KAPALIDIR`,
`PIRLANTA_tipinde_4C_alani_cizilir_ve_karatsiz_Kaydet_KAPALI`) bu kilitleri
doğruluyor ve **mevcut testler değiştirilemez**. Çözüm: kilit korundu ama
**sebebi ekranda yazılır** (Cekmece `KilitSebebi` / `#u-kaydet-kilit`), ve
yeni-kayıt formlarında biçim kuralları (ad/kod/gram/telefon/e-posta) kilitten
alan altı mesaja taşındı. DoD EK md.3'ün amacı (sessiz etkisizlik yok,
görünmez kural yok) karşılanır.

**SAPMA 5 — `KategoriId` de üç duruma çevrildi.** Devir maddesi yalnız
`reyonId` diyor; aynı ekranda aynı hata kategoride de vardı (kullanıcı kategori
seçicisini boşaltınca bağ temizlenmiyordu). Tek dokunuşta birlikte düzeltildi;
hiçbir mevcut test bu davranışa bağlı değildi.

**SAPMA 6 — İç paket numarası sızdıran 3 sözlük metni temizlendi**
(48-A doğrulayıcı devri (a)). Envanter (grep sonucu, TR+EN toplam 6 satır):

| Anahtar | Eski (sızıntılı) | Yeni |
|---|---|---|
| `Duzeltme.StornoHenuzYok` | "…storno henüz yok — **paket 48** ile gelecek…" | "…stok ve maliyet partisi geri alması gerektirdiği için ters kayıtla hükümsüz kılınamaz. Doğru yol: satış/alış için iade…" |
| `Duzeltme.StornoYokGerekce` | "…**paket 48** ile gelecek." | "…stok ve maliyet partisi geri alması gerektirdiği için…" |
| `Duzeltme.UcYokGerekce` | "…düzenleme **paket UX-1 Faz B** ile gelecek." | "Bu kayıt türü şu an yalnız okunabilir; düzeltme için ters kayıt ya da düzeltme fişi kullanılır." |

Ekranda kalan başka `paket N` / `dalga N` / ADR kodu / dosya adı **yok**
(grep `Sozlukler.cs` + `Pages/*.razor`; kalan tek eşleşme
`Fiyatlandirma.razor:15` bir **kod yorumudur**, ekrana basılmaz).

---

## 12. Açık kalanlar

1. **UX-1-B boşluğu — POS ürün seçicide stok adedi yok.** `/urunler` liste yanıtı
   stok taşımıyor; göstermek yeni uç ya da sayfaya yeni GET ister (TB-09).
2. **Kategori `sira` sunucu aralık kontrolü.** İstemcide `min=0`; `KategoriGuncelleDto`
   bu pakette açılmadı (kapsam dışı dosya, tek dokunuş). Negatif sıra hâlâ 201 alır.
3. **Kabuk kalemleri (UX-2-B1'e):** üst bardaki "Kuru güncelle" başarı bildirimi ·
   `kur/guncel` yoklama sıklığı · /giris illüstrasyon-metin çakışması.
4. **Ağaç düğümünde hızlı pasife al/aktife al** (kategori) — reyon için yapıldı,
   kategori satırında hâlâ yalnız Düzenle çekmecesinden.
5. **Stok elle girişi bilinçle açılmadı** (gerekçe ekranda). Açılacaksa ayrı
   paket + ayrı oy: her yol `HareketFabrikasi` ve maliyet partisinden geçmelidir.
6. **Proje ağacında 4 adet eski `.mutorj` artığı var** (`Fiyatlandirma.razor.mutorj`,
   `Islemler.razor.mutorj`, `Sayim.razor.mutorj`, `Urunler.razor.mutorj`,
   `VadeTakibi.razor.mutorj`). **Bu paketin ürünü değildir**, önceki oturumlardan
   kalmıştır; TurkReport hook `rm` engellediği için silinmedi — **kurucuya bırakıldı**.
7. **Tarayıcı turu (375/768/1280 px)** bu pakette yapılmadı; QA ajanına aittir.

---

## 13. Kurucuya / mali müşavire notlar

1. **Satış fişi geri alma politikası (mali müşavir teyidi ister).** Bu sürümde
   kaydedilmiş bir **satış fişi storno edilemez**; geri alma yolu **iade
   işlemidir** (yeni bir hareket doğurur). Storno, tek bacaklı parasal
   hareketlerle (işçilik, veresiye, tahsilat, emanet) sınırlıdır. Ciro ve KDV
   raporlarında iade ile stornonun etkisi farklıdır — mali müşavir bunun
   kabul edilebilir olduğunu teyit etmelidir.
2. **Yalnız açıklaması/ödeme türü yanlış girilmiş bir satış fişi bugün
   düzeltilemiyor.** Ekran bunu gizlemiyor, yazıyor. Tam düzeltme (fiş alanı
   düzenleme ya da stok+maliyet geri alan storno) ayrı bir paket ve ayrı bir
   karardır.
3. **Hiçbir mevzuat hükmü koda sabitlenmedi** bu pakette; tevsik/MASAK eşikleri
   ve kapıları olduğu gibi bırakıldı (yalnız ekran metni ve düzenleme yolu
   değişti).
4. **Ürün fiyatı/teklifi, abonelik, tahsilat gibi ticari metinler üretilmedi**
   (MÜHÜR 2: ücretsiz pilot dışında metin yok).
