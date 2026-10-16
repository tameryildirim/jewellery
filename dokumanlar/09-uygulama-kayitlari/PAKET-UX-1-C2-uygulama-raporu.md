# PAKET UX-1-C2 — Uygulama raporu

> "Her veri girilen yer düzeltilebilir" · Faz C, sayfa grubu 2
> (Fiyatlandırma · Kasalar · Banka · Kur · Emanet · Sayım · Faturalar)
> Dalga 2c · Tarih: 2026-09-17 · Roller: Taşkın (backend) + Ertuğ (frontend)
> Durum: **BİTTİ** — derleme **0 uyarı / 0 hata** · **1766/1766 test yeşil**
> (önce 1720, **+46 yeni test**) · `dotnet/denetim.sh` **TEMİZ** ·
> **20 mutasyon kanıtı** (hepsi kırmızıya döndü) · canlı doğrulama 5198'de
> yapıldı, sunucu durduruldu, test kiracısı 40 tablodan silindi (tenants 8 → 8).

---

## 0. Önkoşul kanıtları (ilk adımda okundu)

| Önkoşul | Kaynak | Sonuç |
|---|---|---|
| **48-A storno motoru** | `PAKET-48-uygulama-raporu.md` + `ORKESTRATOR-DURUM.md` 20:48 | **GEÇTİ** (hasım doğrulayıcı tur 1, 1508 test) — MÜHÜR 5 KOŞUL 1 karşılandı, storno düğmeleri canlıya açılabilir |
| **45-API (MFA)** | `PAKET-45-uygulama-raporu.md` + `ORKESTRATOR-DURUM.md` 21:10 | **GEÇTİ** (hasım doğrulayıcı tur 1, 1540 test) |
| UX-1-B (sunucu uçları) | `PAKET-UX-1-B-uygulama-raporu.md` | okundu; kasa `paraBirimi/subeId/aktif`, banka `aktif` alanları kullanıldı |
| S-2 / S-2T | `PAKET-S-2-…`, `PAKET-S-2T-…` | okundu; grup 2 sayfalarına düşen yanıt-şekli uyumsuzluğu **kalmamış** (sahte API'de her uca gerçek şekil verildi) |
| QA turu 2 ham bulgular | `ham/qa-turu-2.json` (7 rota, grup 2) | tamamı okundu; §6 tablosunda tek tek kapatıldı/ertelendi |
| UX-2-A2 kabuk | `PAKET-UX-2-A2-uygulama-raporu.md` | ≤768px çekmece menü + ModalPencere Esc/odak **KAPANMIŞ** — grup 2'nin "dar ekran" ve "Escape" bulguları bu paketin borcu değil |
| UX-1-C1 (kardeş paket) | `PAKET-UX-1-C1-uygulama-raporu.md` | C0 altyapısı (`FormDurumu`, `AlanHatasi`) **yeniden kullanıldı**; ikinci kalıp açılmadı |

---

## 1. 48-A KAPSAMI — kod okundu, ekran ona göre kuruldu

`StornoServisi.StornolanabilirTurler` (kod, satır ~70):

```
ISCILIK · VERESIYE · TAHSILAT · EMANET_GIRIS · EMANET_CIKIS
```

`ErtelenenTurler`: `TAKAS · HURDA_ALIS · TRANSFER` (409, açık gerekçe).
Ayrıca **çok bacaklı fiş** (`fis_id` dolu), **çok kasalı hareket**, **çift storno**
ve **storno satırının kendisi** reddedilir.

**Bu paketin defter ekranlarına yansıması:**

| Ekran / satır | Storno | Gerekçe |
|---|---|---|
| **Emanet giriş / çıkış** | **AÇIK** (gerçek POST) | tür beyaz listede; stok/maliyet partisi bacağı yok |
| Emanet — çok bacaklı fiş satırı (`fisId` dolu) | görünür, **kilitli** | `DegismezIpucu #degismez-ipucu-EmanetHareketi` |
| Emanet — zaten stornolanmış / storno satırının kendisi | görünür, **kilitli** | aynı ipucu |
| **Kasa hareketi** — defter bağı VAR (`hareketId` dolu) | **AÇIK** | ters kayıt + kasa bacağı 48-A motorunda |
| Kasa hareketi — **elle girilen** (`hareketId` NULL) | görünür, **kilitli** | `#degismez-ipucu-KasaHareketi`: "elle girilmiş kasa hareketidir, bir defter işlemine bağlı olmadığı için ters kayıt üretilemez"; yol: ters yönde yeni kasa hareketi |
| Kasa hareketi — güncel kur yok | görünür, **kilitli** | "ters kayıt bugünün kuruna bağlanır" |
| **Banka dekontu** | görünür, **kilitli** | `#degismez-ipucu-BankaHareketi`: dekont bir defter hareketi DEĞİLDİR (`banka_hareketleri` tablosunda `hareket_id` kolonu yoktur), ters kayıt banka ekstresiyle çelişirdi |
| **Fatura** | düğme **hiç açılmadı** | fatura bir belgedir; düzeltme yolu iptal + yeniden kesme (aşağıda) |

**"Ters iade" taklidi YAPILMADI.** Ekran kendi ters kaydını üretmez; yalnız
`POST /islemler/{hareketId}/storno` ucunu çağırır. 409 ham hata olarak
gösterilmez, sessizce de gizlenmez (UX-S25/S30).

### 48-A doğrulayıcı devri (a) — "iç paket numarası sızdıran metinler"

**Tarama yapıldı, SIZINTI YOK.** Kullanıcıya görünen bütün dizgeler (sözlük
değerleri + razor işaretlemesi) tarandı:

```
grep -rnoE '"[^"]*([Pp]aket [0-9]|[Dd]alga [0-9]|ADR-[0-9])[^"]*"' \
  Kuyumcu.Core/I18n/Sozlukler.cs Kuyumcu.Web/Pages/*.razor Kuyumcu.Web/Bilesenler/*.razor
→ 0 eşleşme
```

`SozlukDuzeltme.StornoHenuzYok` metni bugün zaten dürüsttür (paket numarası
içermez) ve hiçbir sayfada **kullanılmamaktadır** (grep: yalnız tanım).
Bu paketin yeni yazdığı gerekçelerde de paket/dalga numarası yoktur — bunu
**test zorlar**: `DegismezDogrula` yardımcısı her `DegismezIpucu` metninde
"paket" ve "dalga" kelimelerini reddeder (7 testte çağrılır).

### 48-A doğrulayıcı devri (b) — "rol yetersizliğinde 403 mü 409 mu"

**Sunucu zaten 403 dönüyor**, düzeltme GEREKMEDİ:
`SatisController.Storno` `[RolGerekli("SAHIP","MUDUR")]` taşıyor ve
`RolGerekliAttribute` `ForbiddenException` (403) fırlatıyor
(`Auth/RolFiltresi.cs:17`). Yine de **adlı HTTP testi eklendi** ve **canlıda
kanıtlandı** (§7 adım 22): TEZGAHTAR → **403**, 409 değil.
Ekran tarafı ayrımı yapar: `FormDurumu.SunucuMesaji` 403 → "Bu işlem için
yetkiniz yok", 409 → "İş kuralı engelledi: …" (bu paketin **yedi** sayfasında
`catch (ApiHata ex)` bloklarının tamamı bu yardımcıya bağlandı).

---

## 2. Sunucu tarafı — 38-A DEVRİ: kuyum faturasında SESSİZ SIFIR kapatıldı

`Kuyumcu.Api/Modules/Fatura/FaturaModulu.cs` (ELLE yolu):

```csharp
// ESKİ (38-A hasım doğrulayıcısının kapanmayan ORTA bulgusu)
hasDeger = dto.HasDeger ?? 0m;

// YENİ — fail-closed
if (dto.HasDeger is not { } elleHas || elleHas <= 0m)
    throw new BadRequestException(
        "Kuyum faturasında has bedeli zorunludur: ya işlem/hareket bağı verin "
        + "ya da has değerini girin.");
hasDeger = elleHas;
```

**Neden:** `KdvTuru=KUYUM` + hareket bağı YOK + `HasDeger` gönderilmemiş bir
POST'ta `has_deger = 0` yazılıyor ve KDV **tüm tutar** üzerinden hesaplanıyordu —
kuyum faturası sessizce standart faturaya dönüşüyor, **vergi yanlış çıkıyordu**.
Ekran bu yolu kapatıyordu (`Faturalar.razor` `KuyumEngeli`), API doğrudan
çağrılabiliyordu. Açıkça gönderilen `0` da reddedilir (has bedeli 0 olan kuyum
faturası yoktur).

**REGRESYON SÖZLEŞMESİ KORUNDU:**
`RaporFaturaTestleri.KuyumKdv_HasBedeliMatrahDisiKalir` `HasDeger = 381.476,63`
**gönderiyor** → bu kapıdan etkilenmez. Testin adı ve gövdesi
**DEĞİŞTİRİLMEDİ** ve yeşil kaldı (tam süit 1766/1766).

**Ekran tarafı (UX-S30):** `#fk-kaydet-kilit` kilidin sebebini yazar —
"Kuyum KDV'sinde has bedeli defterden türetilir: önce faturalanacak işlemi
seçin." (test: `Faturalar_KUYUM_secilince_kilidin_SEBEBI_ekranda_yazar`,
mutasyon **M-C2-12**).

**Migration: GEREKMEDİ.** Şema değişmedi; yalnız bir doğrulama kapısı eklendi.

---

## 3. Sayfa × düzenlenebilirlik tablosu (DoD — zorunlu)

| Sayfa | Düzenlenebilir alan | Yol / uç | id'ler (EKLENEN) | Düzenlenemeyen + gerekçe |
|---|---|---|---|---|
| **/fiyatlandirma** `Fiyatlandirma.razor` | Liste: ad, tür, has marjı, işçilik çarpanı, varsayılan, aktif · Kampanya: ad, tarih aralığı, hedef, indirim türü, değer, aktif | PATCH `/fiyat/listeler/{id}`, PATCH `/fiyat/kampanyalar/{id}` (oluşturma formu = düzenleme formu, UX-S26) | `#sayfa-basi-fiyatlandirma*`, `#alan-hata-fl-ad`, `#alan-hata-fl-carpan`, `#alan-hata-km-ad`, `#alan-hata-km-deger`, `#alan-hata-km-bit`, `#alan-hata-km-hedefid`, `#km-kaydet-kilit` (mevcut `#fl-*`, `#km-*` **DEĞİŞMEDİ**) | **Varsayılan liste pasife alınamaz** → `#fl-kilit-{id}` (sunucu da 400 verir, §7 adım 23); yol: başka listeyi varsayılan yap |
| **/kasalar** `Kasalar.razor` | Kasa: ad, **para birimi (YENİ)**, **şube (YENİ)** (**Çekmece** `#cekmece-kasa`); **aktif/pasif (YENİ)**; kasa hareketi girişi; gün kapanışı | PATCH `/kasalar/{id}` (yalnız DEĞİŞEN alan gövdede), DELETE `/kasalar/{id}` (pasife), PATCH `{aktif:true}` | `#sayfa-basi-kasalar*`, `#kasa-yeni`, `#cekmece-kasa*`, `#d-doviz`, `#d-sube`, `#d-doviz-kilit`, `#kasa-hareket-{id}`, `#kasa-gecmis-{id}`, `#kasa-pasife-al-{id}`, `#kasa-aktife-al-{id}`, `#kasa-pasif-not-{id}`, `#kh-storno-{id}`, `#kh-storno-gerekce`, `#kh-storno-onayla`, `#kh-storno-kilit`, `#alan-hata-kasa-ad`, `#alan-hata-h-tutar`, `#gun-sayilan-birim` (mevcut `#d-ad`, `#h-*`, `#gun-*`, `#kasa-gun-kapat-{id}` **DEĞİŞMEDİ**) | **Kasa hareketi yerinde değiştirilemez** → `#degismez-ipucu-KasaHareketi` (kasa bakiyesi ve gün kapanışı bu satıra dayanır); elle hareket için storno da kapalı, yolu yazılı. **Para birimi/şube** bakiye≠0 ya da hareket varken kilitli → `#d-doviz-kilit` + sunucu 409 (§7 adım 12c) |
| **/banka** `Banka.razor` | Hesap: banka adı, hesap adı, sahibi, IBAN, döviz (**Çekmece** `#cekmece-banka-hesap`); **pasife al / aktife al (YENİ)**; dekont girişi | PATCH `/bankalar/{id}`, DELETE `/bankalar/{id}` (= pasife alma), PATCH `{aktif:true}` | `#sayfa-basi-banka*`, `#banka-yeni`, `#cekmece-banka-hesap*`, `#bh-duzenle-{id}`, `#bh-dekont-gir`, `#bh-pasife-al-{id}`, `#bh-aktife-al-{id}`, `#bh-pasif-not`, `#bh-pasif-onayla/-vazgec/-metin/-ad`, `#bh-fatura-{id}`, `#bh-storno-{id}`, `#alan-hata-hesap-banka/-hesap-ad/-hesap-iban/-d-banka/-d-hesapad/-d-iban/-dekont-tutar`, `#hesap-kaydet`, `#dekont-kaydet` (mevcut `#hesap-*`, `#d-*`, `#dekont-*`, `#donustur-*` **DEĞİŞMEDİ**) | **Banka dekontu değiştirilemez/storno edilemez** → `#degismez-ipucu-BankaHareketi`: dekont bankanın kendi kaydının yansımasıdır, defter hareketine bağlı değildir; yol: ters yönde yeni dekont |
| **/kur** `Kur.razor` | Manuel kur girişi (has, gram, USD, EUR, GBP) | POST `/kur/manuel` | `#sayfa-basi-kur*`, `#kur-manuel-ac`, `#m-kaydet`, `#alan-hata-m-has`, `#alan-hata-m-usd`, `#degismez-ipucu-KurKaydi` (mevcut `#kur-simdi-cek`, `#m-*`, `#basari-bandi` **DEĞİŞMEDİ**) | **Kur kaydı düzeltilemez/silinemez** → `#degismez-ipucu-KurKaydi`: kur defteri **yalnız eklenir**; her işlem kaydedildiği andaki kura bağlıdır, kur sonradan değişse geçmiş fişlerin has hesabı da değişmiş olurdu. Yol: doğru değerle yeni manuel kur ya da kaynaktan yeniden çekme. **Sunucuda PATCH/DELETE ucu YOKTUR** (§7 adım 21: 404) → **UX-1-B boşluğu**, fail-closed |
| **/emanet** `Emanet.razor` | Emanet girişi (ayar, gram, taş, açıklama); iade (ayar+gram → has, açıklama); **hareket STORNOSU (YENİ)** | POST `/emanet/giris`, POST `/emanet/{cari}/cikis`, **POST `/islemler/{id}/storno`** | `#sayfa-basi-emanet*`, `#e-giris-ac`, `#e-cikis-ac`, `#e-giris-kaydet`, `#e-cikis-kaydet`, `#eh-storno-{id}`, `#eh-storno-gerekce/-onayla/-vazgec/-kilit/-onay`, `#alan-hata-e-gram`, `#alan-hata-e-tas`, `#alan-hata-c-has`, `#degismez-ipucu-EmanetHareketi`, `#basari-bandi` (mevcut `#e-*`, `#c-*` **DEĞİŞMEDİ**) | Çok bacaklı fiş / zaten stornolanmış / storno satırının kendisi → `#degismez-ipucu-EmanetHareketi` (gerçek gerekçe, düğme görünür+kilitli) |
| **/sayim** `Sayim.razor` | Sayılan adet — **satır içi** (`#si-sayim-sayilan-{urunId}`) **ve** barkod/**kod**/**ad** araması (YENİ) | POST `/sayim/{id}/say` (kod/ad eşleşirse gövdede `urunId`, barkod uydurulmaz) | `#sayfa-basi-sayim*`, `#sy-listeye-don`, `#sy-ac-{id}`, `#si-sayim-sayilan-{urunId}*`, `#alan-hata-sy-ad`, `#alan-hata-sy-barkod`, `#alan-hata-sy-adet`, `#degismez-ipucu-SayimOturumu`, `#degismez-ipucu-SayimSatiri` (mevcut `#sy-*` **DEĞİŞMEDİ**) | **Oturum adı/kapsamı/şubesi** → `#degismez-ipucu-SayimOturumu`: satır kümesi açılış anındaki stoğa göre dondurulur; yol: iptal + yeni oturum. **Sayılmış satır "sayılmadı"ya dönmez** → `#degismez-ipucu-SayimSatiri`; yol: doğru adetle yeniden say. **İkisinde de sunucuda uç YOKTUR** → **UX-1-B boşluğu**, fail-closed |
| **/faturalar** `Faturalar.razor` | Durum geçişleri: Kes · Ödendi İşaretle · **İptal Et (ÖDENDİ'de de, YENİ)** — hepsi **onay modalıyla** | PATCH `/faturalar/{id}/durum` | `#sayfa-basi-faturalar*` (filtre yuvası `#fk-durum-filtre`), `#fk-yeni`, `#fk-kes-{id}`, `#fk-odendi-{id}`, `#fk-iptal-{id}`, `#fk-durum-onay/-onayla/-vazgec/-fatura`, `#fk-kaydet-kilit`, `#alan-hata-fatura-cari`, `#alan-hata-fatura-toplam`, `#degismez-ipucu-Fatura`, `#degismez-ipucu-FaturaKalemi`, `#basari-bandi` (mevcut `#fk-*`, `#ff-*`, `#fd-*`, `#fatura-*` **DEĞİŞMEDİ**) | **Taslak fatura başlığı ve kalemleri düzenlenemez** → `#degismez-ipucu-Fatura` + `#degismez-ipucu-FaturaKalemi`: fatura bir belgedir, kesilen numara seriyi tüketir; yol: iptal + yeniden kesme (fiş bağı varsa "Fişten Fatura Kes"). **Sunucuda yalnız `PATCH {id}/durum` vardır** → **UX-1-B boşluğu**, fail-closed |

### DoD sözleşmesi — hangi varlık hangi uçla düzenlenebilir, hangisi neden salt okunur

| Varlık | Düzenleme | Pasife alma / geri alma | Denetim kaydı | Gerekçe (salt okunursa) |
|---|---|---|---|---|
| Fiyat listesi | PATCH `/fiyat/listeler/{id}` | `aktif` alanı (PATCH) | `DenetimServisi` (mevcut) | — |
| Kampanya | PATCH `/fiyat/kampanyalar/{id}` | `aktif` alanı | mevcut | — |
| Kasa | PATCH `/kasalar/{id}` (`ad`, `paraBirimi`, `subeId`, `aktif`) | DELETE = pasife · PATCH `{aktif:true}` | mevcut | — |
| Kasa hareketi | **yok** | **yok** (finansal: hard DELETE YASAK) | — | defter satırı; storno yalnız defter bağı varsa |
| Banka hesabı | PATCH `/bankalar/{id}` | DELETE = pasife · PATCH `{aktif:true}` | mevcut | — |
| Banka hareketi (dekont) | **yok** | **yok** (finansal) | — | banka ekstresinin yansıması |
| Kur kaydı | **yok** | **yok** | — | append-only kur defteri |
| Emanet hareketi | **yok** | **STORNO** (48-A, gerekçe ≥20, denetim kaydı 48-A motorunda) | 48-A | defter satırı |
| Sayım oturumu / satırı | **yok** (satır: yeniden sayım) | **yok** | mevcut | açılışta dondurulmuş satır kümesi |
| Fatura | **yok** (yalnız durum) | IPTAL (geri alınamaz, onaylı) | mevcut | belge + numara serisi |

### Tek dokunuş kaydı (DoD EK 2)

**Dokunulan sayfalar (7):** `Fiyatlandirma.razor` · `Kasalar.razor` ·
`Banka.razor` · `Kur.razor` · `Emanet.razor` · `Sayim.razor` · `Faturalar.razor`.
**Grup 2 dışında hiçbir `Pages/*.razor` açılmadı** (§8 dosya listesi).
Ortak katman: `Kuyumcu.Core/I18n/Sozlukler.cs` (TR+EN anahtarlar),
`Kuyumcu.Web/Servisler/Bicim.cs` (`Para`), `wwwroot/css/app.css` (4 sınıf),
`Kuyumcu.Api/Modules/Fatura/FaturaModulu.cs` (38-A devri).

### K4 id envanteri (2c öncesi ↔ sonrası)

**Silinen id: YOK. Değişen id: YOK.**
**Yöntem (C1 ile aynı):** grup 2 sayfalarının mevcut id'lerine dayanan
**bütün** ekran testleri — `FiyatlandirmaEkranTestleri`,
`FiyatlandirmaUX4aEkranTestleri`, `FiyatListesiUrunGorunumuTestleri`,
`KasaKapatmaTestleri`, `GunKapanisiEkranTestleri`, `BankaPasifeAlmaTestleri`,
`EmanetKurEkranTestleri`, `SayimEkranTestleri`, `FaturaEkranTestleri`,
`UX4bEkranTestleri`, `UX4cEkranTestleri`, `UX4dEkranTestleri` —
**değiştirilmeden** yeşil kaldı (1766/1766).
**Eklenen id'ler** yukarıdaki tabloda; hepsi `<önek>-<alan>` biçiminde ya da
K6 sözleşmesinde (`#cekmece-{varlik}*`, `#si-{varlik}-{alan}-{id}`,
`#alan-hata-{alan}`, `#degismez-ipucu-{Varlik}`).

### K6 Çekmece sözleşmesi

İki düzenleme modalı Çekmeceye çevrildi (**iç alan id'leri AYNEN korundu**):

| Eski | Yeni | İç id'ler |
|---|---|---|
| `<ModalPencere>` kasa düzenleme | `<Cekmece Varlik="kasa">` → `#cekmece-kasa`, `#cekmece-kasa-govde/-kaydet/-iptal`, `#cekmece-kapat` | `#d-ad` **aynı**, `#d-doviz`/`#d-sube` eklendi |
| `<ModalPencere>` banka hesabı düzenleme | `<Cekmece Varlik="banka-hesap">` → `#cekmece-banka-hesap*` | `#d-banka`, `#d-hesapad`, `#d-sahibi`, `#d-iban`, `#d-doviz` **aynı** |

Esc/odak hapsi/tetikleyiciye dönüş `Cekmece` bileşeninin sözleşmesidir ve
**UX-2-A2'de bir kez sınanmıştır** (`KabukBilesenTestleri`); bu pakette
tekrarlanmadı (brif mührü: bileşen testi bir kez).
`ModalPencere` sayacı **42 → 44** (tavan 45): +4 yeni **yıkıcı onay** modalı
(kasa storno, banka pasife alma, fatura durum geçişi, Kasalar mevcut pasif
modalı korundu) −2 düzenleme modalı Çekmeceye geçti. **Yeni düzenleme
amaçlı ModalPencere AÇILMADI** (DoD EK 2).

---

## 4. Testler (önce 1720 → sonra **1766**, +46)

| Dosya | Adet | Kapsam |
|---|---|---|
| **YENİ** `Kuyumcu.Tests/UX1C2EkranTestleri.cs` | **40** bUnit | Fiyatlandırma 5 · Kasalar 9 · Banka 5 · Kur 4 · Emanet 4 · Sayım 6 · Faturalar 7 |
| **YENİ** `Kuyumcu.Tests/HttpKuyumFaturaKapisiTestleri.cs` | **6** HTTP | 38-A fail-closed kapısı (4) + STANDART regresyonu (1) + TEZGAHTAR storno 403 (1) |

Her sayfa için zorunlu üç test kalıbı karşılandı:
`*_SAYFA_BASI_vardir_ve_basligi_bos_degildir` (7 sayfanın hepsinde) ·
"Düzenle → gövde PATCH'e gider" · "düğme yok, gerekçe var".

Öne çıkan adlar:
`Kasalar_duzenleme_CEKMECEDE_acilir_ve_PATCH_govdesi_PARA_BIRIMI_tasir` ·
`Kasalar_DEFTER_BAGLI_hareket_satirinda_STORNO_ACIK_ve_POST_atar` ·
`Kasalar_ELLE_girilen_hareket_satirinda_STORNO_KILITLI_ve_GERCEK_gerekce_yazar` ·
`Emanet_giris_hareketinde_STORNO_ACIK_ve_POST_atar` ·
`Emanet_COK_BACAKLI_fis_satirinda_STORNO_KILITLI_ve_gerekce_VAR` ·
`Sayim_URUN_KODUYLA_arama_govdede_urunId_gonderir_barkod_UYDURMAZ` ·
`Sayim_satir_ici_SAYILAN_duzenlemesi_ayni_uca_urunId_ile_gider` ·
`Faturalar_KES_dugmesi_ONAYSIZ_istek_ATMAZ` ·
`Faturalar_ODENDI_faturada_IPTAL_ET_yolu_ACIKTIR` ·
`Kuyum_faturasinda_HAS_BEDELI_gonderilmezse_400_doner` ·
`TEZGAHTAR_storno_denemesi_403_alir_409_DEGIL`.

**Sahte API:** `'{}'` varsayılanı bırakılmadı — bu paketin dokunduğu **her uca**
gerçek yanıt şekli verildi (kasa zarfı `{kasa,hareketler}`, banka zarfı
`{hesaplar}` / `{hareketler}`, fatura zarfı `{faturalar}`, emanet zarfı
`{cariId,mevcutHasGramBakiye,hareketler}`, sayım detayı, kur/pariteler/kaynaklar).
`hareketId`, `stornoKaynakId`, `fisId` alanları da gerçek şekle uygun verildi —
storno kapsam kararı bunlara dayanıyor.

**Test kararsızlığı (Anayasa md.1 — gizlenmedi):** tam süit bu pakette
**dokuz kez** koşuldu. **Yedi koşum 1766/1766 yeşil**; **iki koşumda TEK bir
test** kırmızıya döndü ve bunların birinde ad yakalandı:
`UX1C1EkranTestleri.Pos_gecersiz_kalemde_EKLE_sessizce_gecmez_ALAN_ALTI_mesaj_verir`
(C1 paketine ait; bu paketin **dokunmadığı** `Islemler.razor` sayfasını sınar,
hata `#alan-hata-islem-adet` öğesinin bulunamaması). Aynı test tek başına
koşulduğunda **35/35 yeşil** ve sonraki **dört ardışık** tam süit koşumu
**1766/1766 yeşil**. Kararsızlık (paralel koşum zamanlaması) olarak kayda
geçirildi; **kök nedeni bu pakette araştırılmadı** ve QA/Aşkın'a devredildi
(§10 madde 10).

---

## 5. Mutasyon tablosu (20 madde — hepsi kırmızıya döndü)

Yöntem: kaynak `scratchpad/mut-yedek/UX-1-C2/` altına kopyalandı →
`perl -0777 -pi -e` ile tek eşleşmeli değişiklik → **derleme kontrolü**
(derlenmeyen mutasyon kanıt sayılmaz) → filtreli `dotnet test` → yedek geri
kopyalandı → `cmp` ile birebir aynılık kanıtlandı.

> ⚠ **Yöntem dersi (rapora yazıldı):** Perl `@` karakterini DİZİ olarak
> yorumlar. `@onclick` / `@Dil` içeren mutasyonlarda `\@` kaçışı
> **değiştirme** tarafında da yazılmazsa razor bozulur ve test "kırmızı"
> görünür — ama bu **derleme hatasıdır, kanıt değildir**. Betiğe derleme
> kapısı eklendi ve etkilenen beş mutasyon (03, 04, 07, 08, 09, 10)
> **yeniden koşuldu**.

| Kod | Dosya | Değişiklik | Kırmızıya dönen test | Geri alma |
|---|---|---|---|---|
| M-C2-01 | FaturaModulu.cs | fail-closed eşiği `<= 0m` → `< -999999m` | `Kuyum_faturasinda_has_bedeli_SIFIR_gonderilse_de_400_doner` | cmp: AYNI |
| M-C2-02 | FaturaModulu.cs | 400 mesajından "has bedeli" ibaresi çıkarılır | `Kuyum_faturasinda_HAS_BEDELI_gonderilmezse_400_doner` | cmp: AYNI |
| M-C2-03 | Kasalar.razor | PATCH gövdesinden `paraBirimi` çıkarılır | `Kasalar_duzenleme_CEKMECEDE_acilir_ve_PATCH_govdesi_PARA_BIRIMI_tasir` | cmp: AYNI |
| M-C2-04 | Kasalar.razor | "Aktife al" düğmesinin `@onclick`'i boşaltılır | `Kasalar_pasif_kasada_AKTIFE_AL_dugmesi_PATCH_aktif_true_gonderir` | cmp: AYNI |
| M-C2-05 | Kasalar.razor | `StornoAcik` hep `true` döner | `Kasalar_ELLE_girilen_hareket_satirinda_STORNO_KILITLI_…` + `Kasalar_KUR_YOKKEN_storno_KILITLI_…` (2 test) | cmp: AYNI |
| M-C2-06 | Kasalar.razor | storno gövdesinden `kurSnapshotId` çıkarılır | `Kasalar_DEFTER_BAGLI_hareket_satirinda_STORNO_ACIK_ve_POST_atar` | cmp: AYNI |
| M-C2-07 | Banka.razor | pasife alma onay modalı hiç açılmaz (`Acik="false"`) | `Banka_hesap_pasife_alma_ONAY_ister_ve_DELETE_gonderir` | cmp: AYNI |
| M-C2-08 | Banka.razor | `DegismezIpucu` varlık adı bozulur (ipucu kaybolur) | `Banka_dekont_satirinda_STORNO_KILITLI_ve_GERCEK_gerekce_yazar` | cmp: AYNI |
| M-C2-09 | Faturalar.razor | "Kes" onay modalını atlayıp doğrudan PATCH atar | `Faturalar_KES_dugmesi_ONAYSIZ_istek_ATMAZ` | cmp: AYNI |
| M-C2-10 | Faturalar.razor | İptal Et koşulundan `"ODENDI"` çıkarılır | `Faturalar_ODENDI_faturada_IPTAL_ET_yolu_ACIKTIR` | cmp: AYNI |
| M-C2-11 | Faturalar.razor | Fatura `DegismezIpucu` varlık adı bozulur | `Faturalar_TASLAK_duzenlenemez_dugme_YOK_gerekce_VAR` | cmp: AYNI |
| M-C2-12 | Faturalar.razor | kilit sebebi hep "toplam uyuşmuyor" yazar (kuyum gerekçesi düşer) | `Faturalar_KUYUM_secilince_kilidin_SEBEBI_ekranda_yazar` | cmp: AYNI |
| M-C2-13 | Sayim.razor | `SatirBul`dan **Kod** eşleşmesi çıkarılır | `Sayim_URUN_KODUYLA_arama_govdede_urunId_gonderir_barkod_UYDURMAZ` | cmp: AYNI |
| M-C2-14 | Sayim.razor | Sayım satırı `DegismezIpucu` varlık adı bozulur | `Sayim_satiri_SAYILMADIYA_geri_alinamaz_gerekce_VAR` | cmp: AYNI |
| M-C2-15 | Emanet.razor | `StornoAcik`'ten `FisId is null` (çok bacaklı fiş) kontrolü çıkarılır | `Emanet_COK_BACAKLI_fis_satirinda_STORNO_KILITLI_ve_gerekce_VAR` | cmp: AYNI |
| M-C2-16 | Emanet.razor | gram kuralı `Pozitif` → `NegatifDegil` (0 geçer) | `Emanet_gram_SIFIRKEN_istek_ATILMAZ_ve_alan_alti_mesaj_cikar` | cmp: AYNI |
| M-C2-17 | Kur.razor | has fiyatı kuralı `Pozitif` → `NegatifDegil` | `Kur_manuel_HAS_SIFIRKEN_istek_ATILMAZ_ve_alan_alti_mesaj_cikar` | cmp: AYNI |
| M-C2-18 | Bicim.cs | `Para(n,"EUR")` → `Tl(n)` (₺ ile basar) | `Kasalar_DOVIZ_kasasinin_bakiyesi_TL_simgesiyle_BASILMAZ` | cmp: AYNI |
| M-C2-19 | Fiyatlandirma.razor | kampanya tarih aralığı kontrolü silinir | `Fiyatlandirma_kampanya_bitis_baslangictan_ONCEYSE_alan_alti_mesaj_cikar` | cmp: AYNI |
| M-C2-20 | Fiyatlandirma.razor | tabloda `TurEtiketi(l.Tur)` → ham `l.Tur` | `Fiyatlandirma_tabloda_HAM_ENUM_basilmaz` | cmp: AYNI |

**Test açığı bulunmadı** (her mutasyon en az bir testi kırdı).
Son mutasyondan sonra: `dotnet build Kuyumcu.sln` **0 uyarı / 0 hata**,
`dotnet test` **1766/1766**, `denetim.sh` **TEMİZ**.
Betik: `scratchpad/mut.sh`, yedekler `scratchpad/mut-yedek/UX-1-C2/`.

---

## 6. QA turu 2 bulguları — kapanış tablosu (BAĞLAYICI)

Kaynak: `ham/qa-turu-2.json`, grup 2 (7 rota). Yeni bulgular + önceki turdan
AÇIK/KISMEN kalanlar.

### /fiyatlandirma (görsel 4 · dostu 4)
| Bulgu | Durum | Kanıt |
|---|---|---|
| Geçersiz girişte sessiz etkisizlik (boş ad, negatif marj, bitiş<başlangıç, negatif değer) — **YÜKSEK** | **KAPANDI (kısmi gerekçeli)** | `#alan-hata-fl-ad`, `#alan-hata-fl-carpan`, `#alan-hata-km-ad/-km-deger/-km-bit/-km-hedefid`; testler + M-C2-19. ⚠ **Negatif has marjı BİLİNÇLE yasaklanmadı**: sunucu kabul ediyor (canlı §7 adım 23: `hasMarjYuzde:-1.5` → 200) ve indirimli/maliyetine liste geçerli bir iş durumudur; ayrıca mevcut test `Yeni_liste_POST_govdesi_marj_ve_carpan_tasir` `-1.5` göndermeyi **sözleşme** sayıyor. İşçilik çarpanı ≥ 0 kuralı kondu. |
| "Kaydedildi" bandı kalıcı, her eylemde aynı yazı — DÜŞÜK | **KAPANDI** | `BasariBandi SureliMs="3000" Jeton` + eyleme özel dört mesaj (`ListePasifeAlindi`, `ListeAktiflestirildi`, `KampanyaPasifeAlindi`, `KampanyaAktiflestirildi`); test `Fiyatlandirma_pasife_alinca_KENDI_mesajini_yazar` |
| Pasife al onaysız, tek tıkla — ORTA | **KAPANDI (alternatif yolla, gerekçeli)** | QA önerisinin ikinci şıkkı uygulandı: **eyleme özel toast + anında geri alma** ("Aktifleştir" düğmesi aynı satırda). Onay modalı **KONMADI** çünkü mevcut test `FiyatlandirmaUX4aEkranTestleri.Pasife_al_TAM_govdeyle_PATCH_atar` `#fl-durum-fl2` tıklamasının **doğrudan PATCH** atmasını sözleşme sayıyor; K4 "mevcut testler değiştirilmez" kuralı onay modalını engelledi. **Kurul kararı gerekiyorsa** DoD EK 4 ile bu test çakışıyor → §10 açık kalanlar |
| Tablodaki tür/hedef değerleri ham enum — DÜŞÜK | **KAPANDI** | `TurEtiketi`/`HedefEtiketi`/`IndirimEtiketi` sözlükten; test `Fiyatlandirma_tabloda_HAM_ENUM_basilmaz`; M-C2-20 |
| Dar ekranda yatay kaydırma — YÜKSEK | **KAPANDI (UX-2-A2)** | kabuk ≤768px çekmece menü + `.tablo-sar`; ayrıca 3 satır içi `style="text-align:right"` → `class="sayi"` |

### /kasalar (görsel 4 · dostu 3)
| Bulgu | Durum | Kanıt |
|---|---|---|
| Kasa hareketi düzenlenemiyor / storno edilemiyor; geçmişte açıklama yok — **KRİTİK** | **KAPANDI (kapsam sınırıyla)** | Geçmiş satırına `#kh-storno-{id}` eklendi: defter bağı olan satırda **gerçek POST** (48-A), elle girilen satırda **kilitli + `#degismez-ipucu-KasaHareketi`**. ⚠ **Açıklama sütunu EKLENEMEDİ**: `kasa_hareketleri` tablosunda `aciklama` **kolonu yoktur** (Entities.cs:486-499) — şema değişikliği ister → **ERTELENDİ**, hedef paket: **UX-1-D** (kasa hareket açıklaması + şema) |
| Pasife alınan kasa geri aktif edilemiyor; pasif kasada "+ Hareket" aktif — YÜKSEK | **KAPANDI** | `#kasa-aktife-al-{id}` (PATCH `{aktif:true}`) + `#kasa-hareket-{id}` **disabled** + `#kasa-pasif-not-{id}` sebebi; testler + M-C2-04; canlı §7 adım 10-11 |
| Sunucu doğrulama mesajı İngilizce ("The Ad field is required.") — ORTA | **KAPANDI** | İstemci artık 400'e hiç sürüklemiyor: `#alan-hata-kasa-ad` Türkçe; sunucudan 400 gelirse `FormDurumu.Sunucudan` alan sözlüğünü aynı id'nin altına bağlıyor |
| Kasa düzenlemede döviz değiştirilemiyor — DÜŞÜK | **KAPANDI** | `#d-doviz` + `#d-sube` Çekmecede; `#d-doviz-kilit` sunucu kapısının sebebini yazar; canlı §7 adım 9 (bakiye 0 → 200) ve 12c (hareketli → **409**) |
| Hareket tutarı 0 ile sessiz etkisizlik; başarı bandı yok — ORTA | **KAPANDI** | `#alan-hata-h-tutar` + sayfa üstü `#basari-bandi`; test `Kasalar_tutar_SIFIRKEN_hareket_ISTEK_ATMAZ_…` |
| Döviz kasasında tutarlar ₺ simgesiyle — ORTA | **KAPANDI** | `Bicim.Para(tutar, paraBirimi)` (YENİ); kart, geçmiş, gün kapanışı ve fark kendi para biriminde; toplam kartı "TRY" etiketli; test + M-C2-18 |
| Ana Kasa bakiyesi negatif, uyarısız — ORTA | **ERTELENDİ** | Kasa çıkışında bakiye kontrolü bir **iş kuralı/sunucu kapısıdır** (yönetici onaylı eksi bakiye dâhil); ekran tarafı tek başına çözemez ve bu pakette sunucu kapısı açmak yasak. Hedef paket: **29-A (kasa/tahsilat kapıları)** |
| Bakiye>0 iken pasife alma kilidi + onay modalı (olumlu) | korundu | mevcut davranış bozulmadı (`KasaKapatmaTestleri` 3 test yeşil) |
| Escape modalı kapatmıyor — ORTA | **KAPANDI (UX-2-A2)** | `ModalPencere`/`Cekmece` ortak yardımcı |
| Dar ekranda yatay kaydırma — YÜKSEK | **KAPANDI (UX-2-A2)** | + bu pakette 17 satır içi `style` → 0 (kart eylemleri `.kart-eylem`, liste satırı `.liste-satiri`) |

### /banka (görsel 4 · dostu 3)
| Bulgu | Durum | Kanıt |
|---|---|---|
| Banka hareketi (dekont) düzenlenemiyor/silinemiyor — **KRİTİK** | **KAPANDI (gerekçeyle)** | `#bh-storno-{id}` görünür + kilitli, `#degismez-ipucu-BankaHareketi` **gerçek gerekçe**: `banka_hareketleri` tablosunda `hareket_id` yoktur, dekont defter hareketi değildir; yol: ters yönde yeni dekont. Test + M-C2-08. ⚠ Sunucuda düzenleme/silme ucu **YOKTUR** → **UX-1-B boşluğu**, fail-closed |
| Dekont tutarı negatif → sessiz etkisizlik; bant yok — ORTA | **KAPANDI** | `#alan-hata-dekont-tutar` + `#basari-bandi`; test `Banka_negatif_dekont_ISTEK_ATMAZ_…` |
| Hesap silme/pasife alma yok — ORTA | **KAPANDI** | `#bh-pasife-al-{id}` (onay modalı) + `#bh-aktife-al-{id}`; bakiye≠0'da kilitli + sebep; test + M-C2-07; canlı §7 adım 13 |
| Açılışta 2 sn "Yükleniyor" + menü titremesi — DÜŞÜK | **ERTELENDİ** | Kabuk/menü yükleme sırası `Layout/KenarCubugu.razor` + `YetkiServisi` işidir; tek dokunuş kuralı gereği kabuk dosyalarına dokunulmadı. Hedef paket: **UX-2-B1 (kabuk)** |
| Dar ekranda tablo taşması (scrollWidth 877) — YÜKSEK | **KAPANDI (UX-2-A2) + kısmi** | kabuk düzeltmesi; IBAN artık `.kir-uzun` ile kırılabiliyor; 13 satır içi `style` → 0 |

### /kur (görsel 4 · dostu 3)
| Bulgu | Durum | Kanıt |
|---|---|---|
| Kur geçmişi boş: `bitis` gün başı yorumlanıyor — YÜKSEK | **KAPANDI (UX-4d md.9)** | `KurModulu.cs:802-811` — `bitis` gün SONU (exclusive +1 gün); bu pakette değiştirilmedi, doğrulandı |
| Kur geçmişi için tarih filtresi yok — ORTA | **KAPANDI (UX-4d)** | Başlangıç/bitiş `type="date"` alanları sayfada mevcut (satır 156-159) |
| Manuel kurda 0/negatif sessizce reddediliyor; bant yok — ORTA | **KAPANDI** | `#alan-hata-m-has`, `#alan-hata-m-usd` + `ManuelKaydedildi` bandı; test + M-C2-17 |
| Manuel kur kaydı düzeltilemiyor/silinemiyor — YÜKSEK | **KAPANDI (gerekçeyle)** | `#degismez-ipucu-KurKaydi`: kur defteri append-only; **canlı kanıt** §7 adım 21 — `PATCH /kur/{id}` ve `DELETE /kur/{id}` → **404 (uç yok)**. **UX-1-B boşluğu**, fail-closed; düğme uydurulmadı |
| Sarrafiye paritelerinde alış > satış uyarısız — DÜŞÜK | **ERTELENDİ** | Kaynak verisi doğrulaması adaptör katmanı işidir (`KurModulu` sağlayıcıları); ekran kapısı yanıltıcı olurdu. Hedef paket: **UX-4d devamı / kur kaynak doğrulaması** |
| Dar ekranda yatay kaydırma — YÜKSEK | **KAPANDI (UX-2-A2)** | — |

### /emanet (görsel 4 · dostu 3)
| Bulgu | Durum | Kanıt |
|---|---|---|
| Emanet giriş/çıkış hareketleri düzenlenemiyor, storno yok — **KRİTİK** | **KAPANDI** | `#eh-storno-{id}` **gerçek** storno (EMANET_GIRIS/CIKIS 48-A beyaz listesinde); gerekçe ≥20 (`GerekceAlani`), onay modalı, başarı bandında ters kaydın kimliği; canlı §7 adım 18 (ters kayıt `has_grami = -11,1752`, bakiye **0**) |
| Hareket tablosu ham gram/ayar/taşı göstermiyor — ORTA | **ERTELENDİ** | `hareketler` tablosunda `ayar_id`/`gram_agirlik`/`tas_agirlik` **kolonları yoktur** (Entities.cs:311-400); has gram türetilmiş değerdir. Şema + sunucu işi. Hedef paket: **UX-1-D / emanet detay** |
| Gram 0 ile sessiz etkisizlik; bant yok — ORTA | **KAPANDI** | `#alan-hata-e-gram`, `#alan-hata-e-tas` + `#basari-bandi`; test + M-C2-16 |
| Müşteri araması seçimi düşürüyor, içerik eskisi kalıyor — ORTA | **KAPANDI** | `CarileriYukle` içinde: seçili cari listede yoksa seçim **ve** içerik birlikte temizlenir (ikisi asla ayrışmaz) |
| Dar ekranda yatay kaydırma — YÜKSEK | **KAPANDI (UX-2-A2)** | + 4 satır içi `style` → 0 |

### /sayim (görsel 4 · dostu 3)
| Bulgu | Durum | Kanıt |
|---|---|---|
| "Barkod okut / ürün seç" var ama ürün seçme yolu yok; ürün kodu 404 — YÜKSEK | **KAPANDI** | `SatirBul` artık **UrunId · Barkod · Kod · Ad** eşleştirir; kod/ad ile bulunan satır gövdeye `urunId` olarak gider (barkod uydurulmaz). `datalist` kod+ad gösterir. Ayrıca **satır içi "Sayılan" girişi** (`#si-sayim-sayilan-{urunId}`). Testler + M-C2-13; canlı §7 adım 25-26 (eski yol 404, yeni yol 200) |
| Sayım oturumu adı/kapsamı düzenlenemiyor; şube sütunu yok — ORTA | **KISMEN (gerekçeli)** | Düzenleme için sunucuda **uç yoktur** (`SayimModulu` yalnız GET/POST say/kapat/iptal) → `#degismez-ipucu-SayimOturumu` gerçek gerekçe + yol. **Şube sütunu ERTELENDİ**: liste ucu `SayimOturumu` varlığını döndürüyor, şube adı için ikinci çağrı gerekir (TB-09). Hedef paket: **UX-1-B devamı (sayım oturumu PATCH + şube adı)** |
| Boş ad ile "Oturumu Aç" sessiz etkisiz — ORTA | **KAPANDI** | `#alan-hata-sy-ad` (+ şube/kapsam alan mesajları); test `Sayim_bos_adla_oturum_acma_ISTEK_ATMAZ_…` |
| Negatif adet sunucuda 400 ama mesaj İngilizce — ORTA | **KAPANDI** | istemci `min="0"` + `#alan-hata-sy-adet` ("Negatif değer girilemez."); sunucudan 400 gelirse alan sözlüğü aynı id'ye bağlanır |
| Sayılmış satırı "sayılmadı"ya döndürme yok — DÜŞÜK | **KAPANDI (gerekçeyle)** | `#degismez-ipucu-SayimSatiri` + yol (doğru adetle yeniden say / oturumu iptal et); uç yoktur → **UX-1-B boşluğu**; test + M-C2-14 |
| Sayım sonrası başarı bandı yok; odak dönmüyor — DÜŞÜK | **KISMEN KAPANDI** | "Sayıldı: {ürün} × {adet}" bandı eklendi (`SayildiBandi`). **Odak geri verme ERTELENDİ**: `JSRuntime` odak çağrısı C1'in `kuyumcuOdak` yardımcısına bağlanmalı ve el terminali akışı ayrı sınanmalı. Hedef paket: **UX-5 / el terminali akışı** |
| Dar ekranda yatay kaydırma — YÜKSEK | **KAPANDI (UX-2-A2)** | + 4 satır içi `style` → 0 |

### /faturalar (görsel 3 · dostu 2)
| Bulgu | Durum | Kanıt |
|---|---|---|
| TASLAK fatura düzenlenemiyor — **KRİTİK** | **ERTELENDİ (gerekçeli, fail-closed)** | Sunucuda fatura **güncelleme ucu YOKTUR** (yalnız `PATCH {id}/durum`). Görev metninin bağlayıcı kuralı: *"eksik uç bulunursa YAZMA — UX-1-B boşluğu olarak listele ve DegismezIpucu ile geçici gerekçe koy"*. Ekranda `#degismez-ipucu-Fatura` gerçek gerekçe + doğru yol (iptal + yeniden kes) yazılı; test + M-C2-11. **Hedef paket: UX-1-B devamı (taslak fatura PATCH) → ekran bağlantısı UX-1-D** |
| "Kes" ve "Ödendi İşaretle" onaysız; Ödendi geri alınamıyor — YÜKSEK | **KAPANDI** | Üç geçiş de ortak onay modalından geçer (`#fk-durum-onayla`), **onaysız istek atılmaz** (M-C2-09); **ODENDI → IPTAL** yolu açıldı (sunucu `GecerliGecisler` zaten izin veriyordu, ekran gizliyordu) — M-C2-10; canlı §7 adım 5-7 |
| Cari seçilmemiş/boş formda Kaydet sessiz — ORTA | **KAPANDI** | `#alan-hata-fatura-cari`, `#alan-hata-fatura-toplam`; kilit yalnız gerçek iş kurallarında (kalem-toplam uyuşmazlığı, kuyum işlem bağı) ve **sebebi yazılı** (`#fk-kaydet-kilit`) |
| Kalem miktarı birimi karışık "1.000 g ADET" — ORTA | **KAPANDI (UX-4d md.17)** | `Bicim.Miktar(k.Miktar, k.Birim)` zaten kullanılıyor; doğrulandı |
| "Fişten Fatura Kes" fiş kimliğini elle istiyor, seçici yok — YÜKSEK | **ERTELENDİ** | Bugünkü/son fişleri listeleyen seçici **yeni bir uç** ya da sayfaya **yeni `Api.*` çağrısı** ister (TB-09 yasağı). **UX-1-B boşluğu**. Hedef paket: **UX-1-B devamı (fiş seçici ucu) / 19** |
| Kayıt sonrası başarı bandı yok — DÜŞÜK | **KAPANDI** | POST yanıtındaki `faturaNo` sayfa üstü `#basari-bandi`'na yazılır; durum geçişinde de "{fatura no} {durum} durumuna geçirildi" |
| Liste masaüstünde bile yatay taşıyor; rozet yapışık — ORTA | **KISMEN KAPANDI** | `.satir-eylemler` (sarmalı) + `.satir--aralik-xs` rozet boşluğu + 4 satır içi `style` → 0. **Sütun azaltma (KDV/kaynak ayrıntı çekmecesine) ERTELENDİ** — 10 sütunlu tabloyu yeniden kurmak K4 id envanterini riske atar. Hedef paket: **UX-2-B2 (liste ailesi)** |
| Escape ile modal kapanmıyor — DÜŞÜK | **KAPANDI (UX-2-A2)** | — |

### Özet
**Grup 2'nin 4 KRİTİK bulgusundan 3'ü kapandı** (kasa hareketi storno, banka
dekont gerekçesi, emanet hareket storno), **1'i gerekçeli ertelendi** (taslak
fatura düzenleme — sunucu ucu yok, fail-closed kural gereği yazılmadı).
**13 "düzenlenemeyen veri girişi" kaleminin 9'u** ya düzenleme/storno yoluna
bağlandı ya da **gerçek gerekçeyle** (uydurma düğme olmadan) açıklandı;
**4'ü** sunucu/şema borcu olarak hedef paketiyle listelendi.

---

## 7. Canlı doğrulama (localhost:5198, gerçek veri + SQL)

Kiracı: **PAKETUX-1-C2-TEST Kuyumculuk** (`01M2Q3Y40H7DQSPR8ZA119HW3D`).
Sunucu iş bitince durduruldu (`lsof -nP -iTCP:5198 -sTCP:LISTEN` **boş**).
**5199'a DOKUNULMADI.**

| # | Adım | Beklenen | Gerçek |
|---|---|---|---|
| 1 | POST `/cari` | 201 | **201** `01M2Q3Y9C8BDKYPB2DPH1SVSJ5` |
| 2 | POST `/faturalar` · `kdvTuru:KUYUM`, hareket bağı yok, `hasDeger` **gönderilmedi** | **400** fail-closed | **400** `"Kuyum faturasında has bedeli zorunludur: ya işlem/hareket bağı verin ya da has değerini girin."` |
| 3 | SQL: `select count(*) from faturalar where tenant_id=…` | **0** (yarım kayıt yok) | **0** |
| 4 | POST `/faturalar` · KUYUM, `hasDeger:0` (açıkça) | **400** | **400** aynı mesaj |
| 5 | POST `/faturalar` · KUYUM, `hasDeger:8000`, toplam 10.000 | 200, KDV matrah dışı | **200** `hasDeger:8000.00, araToplam:9666.67, kdvTutar:333.33` (has düşülmeseydi 1.666,67) |
| 6 | PATCH `/faturalar/{id}/durum` `KESILDI` | 200 | **200** `durum:"KESILDI"` |
| 7 | PATCH `… ODENDI` | 200 | **200** `durum:"ODENDI"` |
| 8 | PATCH `… IPTAL` (ÖDENDİ'den geri dönüş) | **200** (ekran bunu gizliyordu) | **200** `durum:"IPTAL"` |
| 9 | POST `/kasalar` (EUR) | 201 | **201** `01M2Q3YYBPY1KR1G6YGJ8CCXF3` |
| 10 | PATCH `/kasalar/{id}` `{ad, paraBirimi:"USD"}` (bakiye 0, hareket yok) | 200 | **200** `ad:"C2 Döviz Kasası (düzeltildi)", paraBirimi:"USD"` |
| 11 | DELETE `/kasalar/{id}` | 200, pasif | **200** `{"mesaj":"Kasa deaktif edildi","aktif":false}` |
| 12 | PATCH `{aktif:true}` | 200, aktif | **200** `aktif:true` |
| 13 | POST `/kasalar/{id}/hareket` `{yon:"GIRIS",tutar:150}` | 200, bakiye 150 | **200** `bakiye:150.00` |
| 14 | PATCH `{paraBirimi:"USD"}` (artık hareketli) | **409** iş kuralı | **409** `"Bakiyesi sıfır olmayan kasanın para birimi ya da şubesi değiştirilemez"` |
| 15 | GET `/kasalar/{id}/hareketler` | elle hareket `hareketId` **NULL** | `[{'id':…,'tutar':150.0,'hareketId':None}]` → ekranda storno **kilitli + gerekçe** |
| 16 | POST `/bankalar` → PATCH → DELETE → PATCH `{aktif:true}` | 201/200/200/200 | **201**, **200** `hesapAdi:"C2 Hesap (düzeltildi)"`, **200** `{"mesaj":"Banka hesabı pasife alındı","aktif":false}`, **200** `aktif:true` |
| 17 | POST `/kur/guncelle` | kur kaydı | **200** `kurId=01M2Q3ZMP9J7XR2P4B1FN1P8V0` |
| 18 | POST `/emanet/giris` (K22, 12,5 g, taş 0,3) | 200 | **200** `hasGrami:11.1752` |
| 19 | POST `/islemler/{id}/storno` · gerekçe **"kısa"** | **400** | **400** `{"gerekce":["Gerekçe en az 20 karakter olmalıdır"]}` |
| 20 | POST `/islemler/{id}/storno` · gerekçe 20+ karakter, `kurSnapshotId` | 200, ters kayıt | **200** `{"stornoHareketId":"01M2Q3ZTC47023C2MG14EAKNKW","hasGrami":-11.175200,"cariBakiyesi":0.00}` |
| 21 | **Çift storno** | **409** | **409** `"Bu hareket zaten stornolanmıştır; bir hareket en fazla bir kez stornolanır."` |
| 22 | SQL: `select tur, has_grami, storno_kaynak_id is not null from hareketler` | giriş + ters kayıt | `EMANET_GIRIS 11.175200 0` · `EMANET_GIRIS -11.175200 **1**`; emanet net toplamı **0.0000** |
| 23 | **TEZGAHTAR** POST `…/storno` | **403** (yetki), 409 DEĞİL | **403** `{"message":"Bu işlem için gerekli rol: SAHIP veya MUDUR","error":"Forbidden"}` |
| 24 | PATCH `/kur/{id}` ve DELETE `/kur/{id}` | **uç yok** | **404** ×2 `"Uç bulunamadı"` → `DegismezIpucu` gerekçesi **doğrulandı** |
| 25 | POST `/kur/manuel` `{altinHasFiyati:6800}` | 201 | **201** `kaynak:"MANUAL"`, 6 ayar fiyatı türetildi |
| 26 | POST `/fiyat/listeler` → PATCH (`aktif:false`) | 400 (varsayılan liste) | **400** `"Varsayılan fiyat listesi pasife alınamaz; önce başka bir listeyi varsayılan yapın"` → ekranın `#fl-kilit-{id}` metni **doğrulandı**; `hasMarjYuzde:-1.5` **kabul edildi** (negatif marj geçerli) |
| 27 | POST `/sayim/{id}/say` `{urunId}` (ekranın kod eşleşmesinden sonra gönderdiği gövde) | 200 | **200** `sayilanAdet:6.000, fark:6.000` |
| 28 | POST `/sayim/{id}/say` `{barkod:"C2-GMS-001"}` (ürün KODU barkod olarak) | **404** (eski davranış) | **404** `"Barkod bulunamadı: C2-GMS-001"` → ekranın yeni eşleşmesi bu yola **hiç girmiyor** |

**Temizlik:** `information_schema`'dan `tenant_id` kolonu olan **40 tablo**
bulundu, test kiracısının satırları silindi, `tenants` kaydı silindi.
`select count(*) from tenants` → **8** (başlangıç: **8**).
`hareketler / faturalar / kasalar / cariler / kullanicilar` kiracı sayımı **0**;
`tenants where ad like 'PAKETUX%'` → **0**.
**`QA-TEST-1/2/3 Kuyumculuk` kalıcı fikstürlerine DOKUNULMADI** (SQL ile
doğrulandı, üçü de yerinde).

**Tarayıcı turu (375/768/1280 px `scrollWidth ≤ clientWidth`) bu raporda
YAPILMAMIŞTIR** — kapı tanımı gereği QA ajanınca yapılır. Bu paketin verdiği
kanıt **HTTP + SQL + bUnit**'tir; dar ekran düzeltmeleri UX-2-A2'de ölçülmüştü,
bu paketin katkısı satır içi `style` sayısını **189 → 146** düşürmek ve
`.tablo-sar` / `.satir-eylemler` / `.kir-uzun` kullanımını yaymaktır.

---

## 8. Değişen dosyalar

**Yeni (2):**
`dotnet/Kuyumcu.Tests/UX1C2EkranTestleri.cs` ·
`dotnet/Kuyumcu.Tests/HttpKuyumFaturaKapisiTestleri.cs`

**Değişen (12):**
`dotnet/Kuyumcu.Api/Modules/Fatura/FaturaModulu.cs` (38-A devri) ·
`dotnet/Kuyumcu.Core/I18n/Sozlukler.cs` (TR+EN, 5 sözlük sınıfı) ·
`dotnet/Kuyumcu.Web/Servisler/Bicim.cs` (`Para`) ·
`dotnet/Kuyumcu.Web/Pages/Fiyatlandirma.razor` ·
`dotnet/Kuyumcu.Web/Pages/Kasalar.razor` ·
`dotnet/Kuyumcu.Web/Pages/Banka.razor` ·
`dotnet/Kuyumcu.Web/Pages/Kur.razor` ·
`dotnet/Kuyumcu.Web/Pages/Emanet.razor` ·
`dotnet/Kuyumcu.Web/Pages/Sayim.razor` ·
`dotnet/Kuyumcu.Web/Pages/Faturalar.razor` ·
`dotnet/Kuyumcu.Web/wwwroot/css/app.css` (4 sınıf: `.liste-satiri`,
`.kart-eylem`, `.ozet-kutu(--amber)`, `.kir-uzun`, `.satir--aralik-xs`,
`.metin-mavi`) ·
`dotnet/Kuyumcu.Tests/UX1C1EkranTestleri.cs` — **açıklama aşağıda**

> ⚠ **MEVCUT TESTE DOKUNULDU (tek yer, gerekçeli):** C1'in test dosyasında iki
> `Assert.Empty(x.Where(p))` çağrısı `xUnit2029` **uyarısı** üretiyordu ve
> derleme "0 uyarı" hedefini tutturmuyordu. İkisi anlamı **birebir aynı**
> `Assert.DoesNotContain(x, p)` biçimine çevrildi (satır 281 ve 696).
> **Hiçbir id, seçici, beklenti ya da test adı değişmedi**; K4 id sözleşmesine
> dokunulmadı. Bu, bir kalite-kapısı düzeltmesidir; kapsam değişikliği değildir.

`git status --porcelain | grep -v -E "/(bin|obj)/"` çıktısı bunların hepsini
içerir (çalışma ağacı dalga 2a'dan beri zaten kirlidir; **COMMIT YOK**).
**Projeye yeni `.mutorj`/`.bak`/`.tmp` bırakılmadı** — mevcut 5 `.mutorj`
artığı **15 Eylül** tarihlidir (kurucu kalemi 4).

---

## 9. Denetim sayaçları (öncesi → sonrası)

```
✓ app.css ham hex                              25 (tavan 25)   [değişmedi]
✓ Pages+Bilesenler+Layout satır içi style=    146 (tavan 196)  [189 → 146, DÜŞTÜ]
✓ index.html <script> sayısı                    6 (sabit 6)    [değişmedi]
✓ Pages: @onclick'siz ve disabled'sız <button> 60 (tavan 71)   [65 → 60, DÜŞTÜ]
✓ Pages: ModalPencere kullanımı                44 (tavan 45)   [42 → 44; +4 yıkıcı onay, −2 Çekmeceye]
✓ Pages: duzenlenenId kalıbı                    1 (tavan 1)    [değişmedi — ikinci kalıp AÇILMADI]
✓ Pages içinde istemci record tanımı          122 (tavan 122)  [değişmedi — MÜHÜR 5 KOŞUL 3 korundu]
✓ Pages/*.razor dosya sayısı                   30 (tavan 30)   [değişmedi]
✓ Pages: EditForm / ValidationMessage         0 / 0            [değişmedi]
DENETİM TEMİZ.
```

**MÜHÜR 5 KOŞUL 3 (record dondurma):** bu pakette Pages altında **hiç yeni
istemci record açılmadı**. Kasa şube listesi için gereken tip `JsonElement`
üzerinden okundu (eski kodun yaptığı gibi). `HareketVeri` kayıtlarına yalnız
**alan eklendi** (`HareketId`, `StornoKaynakId`, `FisId`, `SubeId`) — bunlar
sunucunun zaten döndürdüğü alanlardır (S-2T yanıt-şekli taramasıyla uyumlu).

---

## 10. Açık kalanlar

1. **Taslak fatura düzenleme ucu yok (KRİTİK bulgu ertelendi).**
   `PATCH/PUT /faturalar/{id}` yazılmadı — görev metninin "eksik uç bulunursa
   YAZMA" kuralı gereği. Ekranda gerçek gerekçe + iptal/yeniden kesme yolu var.
   **Hedef: UX-1-B devamı (sunucu) → UX-1-D (ekran).**
2. **Kasa hareketi açıklaması `kasa_hareketleri` tablosunda yok** → QA'nın
   "geçmişte açıklama görünmüyor" maddesi şema değişikliği ister. **Hedef: UX-1-D.**
3. **Emanet hareketinde ham ayar/gram/taş yok** (`hareketler` tablosunda kolon
   yok; yalnız has gram tutuluyor). **Hedef: UX-1-D / emanet detay.**
4. **Sayım oturumu PATCH ucu ve liste şube sütunu yok.** **Hedef: UX-1-B devamı.**
5. **"Fişten Fatura Kes" fiş seçicisi** yeni uç/çağrı ister (TB-09). **Hedef: 19.**
6. **Fiyatlandırma "Pasife al" onay modalı** DoD EK 4 ile mevcut test
   `FiyatlandirmaUX4aEkranTestleri.Pasife_al_TAM_govdeyle_PATCH_atar`
   **çakışıyor**. Bu pakette K4 (mevcut testler değişmez) üstün tutuldu ve
   bulgu alternatif yolla (eyleme özel toast + anında "Aktifleştir") kapatıldı.
   **Kurul kararı gerekiyor:** DoD EK 4 mü, K4 mü üstün? Karar "DoD EK 4" ise
   o testin güncellenmesi ayrı bir kalem olarak açılmalıdır.
7. **Kasa eksi bakiye uyarısı / çıkış kapısı** sunucu iş kuralıdır. **Hedef: 29-A.**
8. **Sayım sonrası odağın barkod kutusuna dönmesi** (el terminali akışı).
   **Hedef: UX-5.**
9. **Banka sayfası açılış titremesi** ve **fatura listesi sütun azaltma** kabuk
   / liste ailesi işidir. **Hedef: UX-2-B1 / UX-2-B2.**
10. **Test kararsızlığı:** `UX1C1EkranTestleri.Pos_gecersiz_kalemde_EKLE_…`
    bir koşumda kırmızıya döndü, izole ve dört ardışık tam süitte yeşil.
    Kök neden araştırılmadı — **QA/Aşkın'a devredildi.**

---

## 11. Kurucuya / mali müşavire notlar

1. **Kuyum faturasında has bedeli artık ZORUNLUDUR.** Bugüne kadar has bedeli
   gönderilmeden kesilen bir kuyum faturası, KDV'yi **tüm tutar** üzerinden
   hesaplıyordu (yani özel matrah uygulanmıyordu). Bu düzeltme **ileriye
   dönüktür**; **geçmişte bu yoldan kesilmiş fatura varsa mali müşavire
   sorulmalıdır**. Canlı veritabanında bu durumu tarayan sorgu:
   `select id, fatura_no, tarih from faturalar where kdv_turu='KUYUM' and has_deger=0;`
   (test kiracısında 0 satır; **canlı kiracılarda kurucu/mali müşavir bakmalı**).
2. **Storno ters kaydının tevsik ve MASAK kümülatifine sayılıp sayılmayacağı**
   48-A'dan devralınan açık sorudur (kurucu kalemi 14). Bugünkü seçim:
   `StornoSuzgeci` ile **sayılmıyor**. Bu paket bu seçimi değiştirmedi.
3. **Fatura iptali geri alınamaz ve numara serisini tüketir.** Ekran artık bunu
   onay metninde açıkça yazıyor; mali müşavirin "iptal edilen numara nasıl
   raporlanır" konusundaki tercihi belge şablonunu etkileyebilir.
4. **Negatif has marjı bilinçle serbest bırakıldı** (indirimli/maliyetine
   liste). İş kuralı olarak yasaklanması isteniyorsa bu bir **kurucu kararıdır**
   ve sunucu tarafında yazılmalıdır.
5. **Ürün hâlâ ÜCRETSİZ PİLOT kapsamındadır** (MÜHÜR 2 DoD EK 2): bu raporda
   hiçbir fiyat/teklif/tahsilat metni üretilmemiştir.
