# PAKET B-1 — GÖRÜNÜM SUNUCU TARAFI (8 şema + 3 yazı boyutu + kullanıcı tercihi)

> **Dalga:** S-B BACKEND ŞEKİLLENDİRME · **Roller:** Taşkın (backend) + Ertuğ (frontend) · **Tarih:** 23 Eylül 2026
> **Durum:** BİTTİ · **Proje kökü:** `/Users/ramazanakkocan/ByGatech-yerel/jewellery`
> (⚠ `~/Desktop/ByGatech/projeler/jewellery` bu dizine **sembolik bağdır** — `CLAUDE.md` inode'u ikisinde de 37195780; ölçüldü, varsayılmadı.)

---

## 0. Devralma — bu oturum neyi devraldı, neyi tamamladı

Şerit 23.09 18:50'de kurucunun "internet gidecek" uyarısıyla **elden temiz durdurulmuştu**. ORKESTRATÖR devir notu
(`ORKESTRATOR-DURUM.md § 23.09 18:55 PLANLI DURUŞ`) 16 kırmızının **15'ini B-1'e** yazmıştı
(UX1C4EkranTestleri 8 + SurumEkranTestleri 6 + AyarlarTemaEkranTestleri 1 — hepsi `Pages/Ayarlar.razor`'a bakıyor).

**Ölçülen devir hali (bu oturumun ilk komutları):** `dotnet build Kuyumcu.sln` → 0 Uyarı 0 Hata ·
B-1 yakın çevresi filtreli koşum → **Başarısız 15 / Başarılı 68 / Toplam 83** ·
`bash dotnet/denetim.sh` → **1 ihlal: P-11 DIS_HATADA_VERI_DUSUREN_YOL_TOPLAM 48 > tavan 47**.

Devralınan işin **sunucu tarafı zaten yazılmıştı** (önceki ajan): migration, `GorunumSecenekleri` 8/3/3 listeleri,
`KullaniciGorunumModulu` (GET+PATCH), `KullaniciGorunumServisi` (Web), `Ayarlar.razor` "Benim görünümüm" kartı,
`B1GorunumEkranTestleri` + `B1KullaniciGorunumTestleri`. Bu oturumun işi **yarım kalan izi kapatmak** oldu:

| Yarım kalan | Bu oturumda ne yapıldı |
|---|---|
| 15 kırmızı bUnit testi | `Ayarlar.razor` artık `KullaniciGorunumServisi` enjekte ediyor; **eski 3 bUnit fikstürü onu DI'ya kaydetmiyordu** → `InvalidOperationException: There is no registered service…`. Üç fikstüre kayıt eklendi ve sahte API'lerine **gerçekçi** `/kullanici/gorunum` yanıtı verildi (yeni yardımcı: `B1SahteGorunumYaniti.cs`). |
| denetim ihlali 48 > 47 | Sayaca takılan **3 satırdan biri B-1'in kendi kodu**ydu (`Ayarlar.razor` → `benimBasari = null;` bir `catch` bloğunun içindeydi). Ölçüldü, B-1'e ait olduğu için **düzeltildi**: başarı bandı temizliği `try`'dan ÖNCE'ye alındı; davranış birebir aynı, sayaç 48 → **47**. |

⚠ **Bu pakete AİT OLMAYAN kırmızıya dokunulmadı:**
`OlcumKapisiTestleri.denetim_sh_HER_CAGRI_DIZININDEN_AYNI_KARARI_VERIR` (sahibi **P-30b**, DALGA 7c) hâlâ kırmızıdır.
Gerçek hata iletisi: `Assert.Contains() Failure: Sub-string not found · Not found: "ÖLÇÜLEMEDİ(betik kendi yolundan okunamadı"`.
Aynı dosyanın (`OlcumKapisiTestleri.cs:525`) 2 adet `CS8604` uyarısı da P-30b'nindir (B-1 bu dosyaya dokunmadı).

---

## 1. Yapılanlar (B-1'in bütünü — devralınan + bu oturum)

### 1.1 Veri modeli ve migration
`kullanicilar` tablosuna **üç nullable kolon**: `tema_modu` varchar(12) · `sema` varchar(24) · `yazi_boyutu` int.
**NULL = "bilinmiyor" değil, "DEVRAL" demektir** — hiçbir mevcut satır doldurulmadı, kimsenin ekranı sessizce değişmedi.

Migration: **`20260923135612_B1KullaniciGorunumTercihi`** · `.cs` 3.916 bayt, `.Designer.cs` 235.611 bayt (ikisi de > 0).
`Down()` üç kolonu da düşürür → **geri alınabilir**.

**Canlı uygulama kanıtı (`kuyumcu_net`):**
```
SELECT COUNT(*) FROM `__EFMigrationsHistory` WHERE MigrationId LIKE '%B1KullaniciGorunumTercihi';  → 1
SELECT COUNT(*) FROM `__EFMigrationsHistory`;                                                      → 69
information_schema → kullanicilar kolonları: sema, tema_modu, yazi_boyutu
```

### 1.2 Göç haritası — ESKİ DEĞER ÖLMEZ
Hem migration `Up()` içindeki üç `UPDATE` ile (yeniden çalıştırılabilir: `WHERE` yalnız eski kodları tutar),
hem de `GorunumSecenekleri.*TakmaAdlari` ile **çalışma zamanında** kabul edilir (eski istemci 400 almaz):

| Eski değer | Yeni değer | Gerekçe |
|---|---|---|
| `altin` | `platin` | Altın/amber objektif testten geçemedi (TEMA-SİSTEMİ § 2) |
| `kurumsal` | `platin` | "Kurumsal mavi" → "Platin Mavi" |
| `platin-mavi` (belge adı) | `platin` | **Ölçüm kazandı** — canlı CSS seçicisi `[data-sema="platin"]` |
| `yogun` | `16` | |
| `okunakli` | `20` | **Ölçüm kazandı** — canlı CSS `[data-yazi="20"],[data-olcek="okunakli"]`; kullanıcının BUGÜN gördüğü boyut korunur |
| `otomatik` (belge adı) | `sistem` | **Ölçüm kazandı** — canlı `TemaServisi`/`js/tema.js` `"sistem"` yazıyor |

### 1.3 Listeler — 8 şema · 3 tema modu · 3 punto
`platin (varsayılan) · zumrut · grafit · bordo · okyanus · menekse · zeytin · fildisi` ·
`sistem (varsayılan) · aydinlik · karanlik` · `16 · 18 (varsayılan) · 20`.
Tek kaynak: `GorunumSecenekleri` (sunucu); `Kuyumcu.Web/Servisler/GorunumServisi.cs` aynadır ve eşitlik testle kilitlidir.

### 1.4 Uçlar (yalnız EKLENDİ)
| Fiil | Yol | Kapı |
|---|---|---|
| GET | `/api/v1/kullanici/gorunum` | `[HerkeseAcik]` — gerekçeli |
| PATCH | `/api/v1/kullanici/gorunum` | `[HerkeseAcik]` — gerekçeli; hedef kimlik **jetondan**, gövdeden değil |
| (mevcut) GET/PATCH | `/api/v1/gorunum` | dükkân varsayılanı, SAHİP kilidinde — **değiştirilmedi** |

### 1.5 Ekran
`Pages/Ayarlar.razor` → `#ayar-benim-gorunum` kartı: üç seçici (şema/tema modu/punto), her birinin ilk seçeneği
**"Dükkân varsayılanı" = devralma (silme) yolu**, kaynak rozetleri (`kullanici`/`kiraci`/`sistem` — UX-S21),
başarı + hata bandı (UX-S27/S28), okunamadıysa kilit + sebep (UX-S30), `#benim-kagit-notu` "Kâğıt daima beyaz" notu.
Sözlük **ayrı dosyada**: `Kuyumcu.Core/I18n/GorunumSozlugu.cs` (ortak hub `Sozlukler.cs`'e **dokunulmadı**).

### 1.6 Seçim herkese açık
Tuval notu **n4** birebir: *"her kullanıcı kendi temasını, aydınlık/karanlık tercihini, yazı boyutunu (16/18/20 px) …
seçer; sahip dükkân varsayılanını belirler."*
`BenimKilitli => !BenimGorunum.Yuklendi` (rol yok) · `GorunumKilitli => !SahipMi || !Gorunum.Yuklendi`
(**yalnız kiracı varsayılanı** için) — Ayarlar.razor 519 ve 556. satırlar.

---

## 2. Sapmalar ve gerekçeleri

| # | Sapma | Gerekçe |
|---|---|---|
| S-1 | **Varsayılan punto 18, TEMA-SİSTEMİ § 7'nin dediği 16 değil** | Görev metni "taslak varsayılanı 18" diyor ve **TEK DOĞRULUK KAYNAĞI** kurucunun onayladığı tuvaldir: `tuval/project/Ayarlar.dc.html:17` kök `font-size: 18px`. Ayrıca canlı `belirtecler.css` `:root{--yazi-govde:18px}` yazıyor — sunucu 16 deseydi kullanıcı ekranda 18 görüp ayarda "16" okurdu. |
| S-2 | **Şema slug'ı `platin`, belgedeki `platin-mavi` değil** | Ölçüm: S-1 şeridi CSS'e `[data-sema="platin"]` indirmiş. Belgedeki ad **takma ad** olarak kabul edilir (400 dönmez). |
| S-3 | **Tema modu `sistem`, belgedeki `otomatik` değil** | Canlı `TemaServisi` + `js/tema.js` + localStorage `"sistem"` yazıyor; `otomatik` takma ad. |
| S-4 | **`okunakli` → 20 px (belge 18 derdi)** | Canlı CSS `[data-yazi="20"],[data-olcek="okunakli"]`. Göç, kullanıcının BUGÜN gördüğü boyutu korur. |
| S-5 | **CSS/belirteç dosyalarına dokunulmadı** | `belirtecler.css`/`app.css`/`Layout/*` **S-1 şeridinindir** (izolasyon kuralı). B-1 "GÖRÜNÜM SUNUCU TARAFI"dır. |
| S-6 | Mevzuat araştırması yapılmadı | Bu paket **hiçbir mali/mevzuat hükmü içermez** (renk/punto tercihi). Mevzuat kuralı yazılmadı → doğrulanacak hüküm yok. |

---

## 3. DoD sözleşmesi

| Varlık / kayıt | Düzenleme yolu | Silme / devralma | Yetki | Denetim |
|---|---|---|---|---|
| **Kullanıcı görünüm tercihi** (`kullanicilar.tema_modu/sema/yazi_boyutu`) | `PATCH /api/v1/kullanici/gorunum` — gövdede yoksa **korunur** | Alan `null` gönderilince **dükkân varsayılanına devredilir** (kullanıcı satırı temizlenir) = silme yolu | `[HerkeseAcik]` + kimlik **jetondan** (başkasının satırını yazma yolu YOK) | `DenetimServisi.YazmaKaydet` **çağrılır**, `varlik_turu = KullaniciGorunum` |
| **Dükkân görünüm varsayılanı** (`kiraci_ayarlari`) | `PATCH /api/v1/gorunum` (mevcut, P-36b) | `null` → ayar satırı silinir, sistem varsayılanına döner | SAHİP kilidi (`GorunumKilitli`) | mevcut |
| **Salt okunur olan** | — | — | — | Seçenek listeleri (`semalar/temaModlari/yaziBoyutlari`) **sunucu sabitidir**; ekranda `DegismezIpucu` yerine listeden gelir, kullanıcı yeni şema üretemez (palet 496 kontrast ölçümüyle kapalıdır — TEMA-SİSTEMİ § 6). |

**Finansal varlık yok** → hard DELETE yasağı bu pakete uygulanmaz; tercih satırı temizlemek defteri bozmaz.
**Ekranda düğmesi olmayan uç yok**, **isteği olmayan düğme yok** (BE3/BE4 testleri gerçek PATCH'i sayar).

---

## 4. Derleme

```
dotnet build Kuyumcu.sln --nologo     →  0 Uyarı, 0 Hata   (son koşum, exit=0)
```
⚠ **Dürüstlük notu:** `Kuyumcu.Tests` tam yeniden derlendiğinde **2 adet `CS8604`** çıkıyor
(`Kuyumcu.Tests/OlcumKapisiTestleri.cs:525`, `P29Satiri(cikti)` için olası null). Bu dosya **P-30b'nindir**,
B-1 ona dokunmadı; artımlı derlemede görünmüyor.

## 5. Testler (önce / sonra)

| Ölçüm | Önce | Sonra |
|---|---:|---:|
| B-1 çevresi filtreli koşum (7 sınıf) | 15 başarısız / 68 başarılı / **83** | **0 başarısız / 83 başarılı / 83** |
| Tam süit | 16 başarısız / 4019 başarılı / **4035** (devir notu) — bu oturumda ilk ölçüm **4035, 1 başarısız** | **1 başarısız / 4042 toplam** |
| Yeni test sayısı (B-1 bu oturumda) | — | **0** (test **silinmedi**; toplam 4035 → 4042 arttı) |

⚠ **Toplam neden 4035 → 4042 arttı?** B-1 test EKLEMEDİ. Ağaçta **eşzamanlı başka şerit yazıyor**:
`Kuyumcu.Tests/MarkaAdiTestleri.cs` 18:10'da (B-1 dışı bir ajan tarafından) değişti. Sayı **düşmedi**, arttı — koşul karşılandı.

**Kalan tek kırmızı:** `OlcumKapisiTestleri.denetim_sh_HER_CAGRI_DIZININDEN_AYNI_KARARI_VERIR` — **P-30b'nin yarım işi**,
B-1'e ait değil, devir notu gereği dokunulmadı (bkz. § 0).

## 6. Mutasyon kanıtları

Yöntem: kaynak `scratchpad/mut-yedek/B-1/` altına kopyalandı → `perl -0777 -pi -e` / `python3` ile TEK eşleşmeli değişiklik →
filtreli `dotnet test` → yedek geri kopyalandı → `cmp` ile birebir aynı olduğu kanıtlandı. Son mutasyondan sonra TEMİZ `dotnet build`.

| Kod | Değişiklik | Kırmızıya dönen test | Geri alındı (cmp) |
|---|---|---|---|
| **M-B1-1** | `GorunumSecenekleri.SemaGecerli` → her zaman `true` (beyaz liste kaldırıldı) | `B1KullaniciGorunumHttpTestleri.B1H7_Beyaz_liste_disi_deger_400_doner(sema/pembe)` + `B1GorunumSecenekleriTestleri.B1_3_Bilinmeyen_deger_null_doner` | AYNI |
| **M-B1-2** | `KullaniciGorunumServisi.Yama` → `if (dto.SemaVerildi)` yerine `if (true)` ("gövdede yoksa koru" kuralı silindi) | `B1H5_Gonderilmeyen_alan_korunur` | AYNI |
| **M-B1-3** | `SemaTakmaAdlari`'ndan `"kurumsal"` göçü kaldırıldı | `B1_2_Eski_kodlar_yeni_slug_a_gocer` | AYNI |
| **M-B1-4** | `Pages/Cikti.razor` sonuna `<div data-sema="platin"></div>` (kâğıt temaya bağlandı) — derleme YEŞİL kaldı | `B1_4_Kagit_daima_beyaz_yazdirma_kaynaklari_temaya_baglanmaz` | AYNI |
| **M-B1-5** | `Getir` → `Sema: ozelSema ?? kiraci.Sema` yerine `Sema: kiraci.Sema` (kullanıcı katmanı yok sayıldı) | `B1H4_Kullanici_satiri_dukkan_varsayilanini_ezer` + `B1H5` | AYNI |
| **M-B1-6** | `TemaEkranTestleri.cs`'ten bu oturumda eklenen `AddScoped<KullaniciGorunumServisi>()` satırı silindi | `AyarlarTemaEkranTestleri.Ayarlar_sayfasinda_tema_karti_ve_secici_VARDIR` | AYNI |
| **M-B1-7** | `Ayarlar.razor` → `benimBasari = null;` yeniden `catch` bloğunun İÇİNE alındı | **test değil, kapı:** `denetim.sh` → `✗ P-11 … 48 > tavan 47`, `DENETİM BAŞARISIZ` | AYNI |

**Test açığı bulunmadı** — yedi mutasyonun yedisi de kırmızı/ihlal üretti.

## 7. Denetim

```
$ bash ./denetim.sh          (dotnet/ içinden, BASH ile)
  ✓ P-11: DIS_HATADA_VERI_DUSUREN_YOL_TOPLAM       47 (tavan 47 — cırcır YALNIZ azalır)
DENETİM TEMİZ.                                        exit=0 · 272 adet ✓
```
(Devirdeki `✗ 48 > 47` ihlali **kapandı** — § 0.)

## 8. Canlı doğrulama (port **5193**, gerçek veri)

Kiracı **PAKETB-1-TEST** (`01M37DVPB0ZXFDHWBK9ZRXN7DJ`) · `b1.sahip@test.local` (SAHİP).
API: `ASPNETCORE_ENVIRONMENT=Development … --no-build --urls http://localhost:5193` (5194–5200 arasına **dokunulmadı**).

| # | Adım | Beklenen | Gerçek |
|---|---|---|---|
| 1 | `POST /sistem/davetler` (gerekçeli) | 201 | **201** |
| 2 | `POST /auth/kayit` (PAKETB-1-TEST) | 201 | **201**, `tenantId=01M37DVPB0ZXFDHWBK9ZRXN7DJ` |
| 3 | `GET /kullanici/gorunum` | 200, sistem varsayılanı + 8/3/3 liste | **200** · `sema:"platin" temaModu:"sistem" yaziBoyutu:18`, kaynaklar `sistem`, `semalar` **8**, `temaModlari` **3**, `yaziBoyutlari [16,18,20]` |
| 4 | `PATCH {"sema":"menekse"}` | 200, kaynak `kullanici` | **200** · `sema:"menekse"`, `semaKaynagi:"kullanici"`, `ozelSema:"menekse"` |
| 5 | `PATCH {"yaziBoyutu":20}` (sema **gönderilmedi**) | 200, **sema KORUNUR** | **200** · `sema:"menekse"` korundu, `yaziBoyutu:20` · SQL: `b1.sahip@test.local NULL menekse 20` |
| 6 | `PATCH {"sema":null}` | 200, dükkân varsayılanına **devredilir** | **200** · `sema:"platin"`, `semaKaynagi:"sistem"`, `ozelSema:null` |
| 7 | `PATCH {"sema":"pembe"}` | **400** | **400** `"Bilinmeyen renk şeması: pembe"` |
| 8 | `PATCH {"temaModu":"karanlik"}` | 200, kalıcı | **200** · `ozelTemaModu:"karanlik"` · SQL: `tema_modu=karanlik` |
| 9 | `PATCH {}` (boş gövde) | **400** | **400** `"Hiçbir alan gönderilmedi — değiştirilecek bir şey yok."` |
| 10 | `PATCH {"sema":"kurumsal"}` (eski kod) | 200 + kanoniğe göç | **200** · `ozelSema:"platin"` |
| 11 | **MEVCUT UÇ** `GET /gorunum` | 200 | **200** `{"sema":"platin","yaziOlcegi":"18","temaModu":"sistem"}` |
| 12 | **MEVCUT UÇ** `PATCH /gorunum {"sema":"zumrut"}` | 200 | **200** `{"gorunum":{"sema":"zumrut",…},"mesaj":"Görünüm kaydedildi"}` |
| 13 | Denetim izi | `KullaniciGorunum` kaydı var | **`KullaniciGorunum EKLEME 5`** · detay: `Kullanıcı görünümü güncellendi: renk şeması` / `… tema modu` |

**Temizlik:** API durduruldu (`lsof -nP -iTCP:5193 -sTCP:LISTEN` → **0 satır**), test kiracısı `tenant_id` kolonu olan
**70 tablodan** silindi + `tenants` + `kiraci_davetleri` satırı silindi.
`SELECT COUNT(*) FROM tenants` → **9** (başlangıçtaki değer) · kiracıya ait `kullanicilar`/`denetim_kayitlari` → **0**.
`QA-TEST-` önekli kalıcı fikstürlere **dokunulmadı**.

## 9. "Mevcut hali bozulmasın" — altı koşulun kanıtı

| # | Koşul | Durum | Kanıt |
|---|---|---|---|
| 1 | `dotnet build` 0 Hata (0 Uyarı hedef) | **KARŞILANDI** | exit=0 · `0 Uyarı, 0 Hata` (tam yeniden derlemede 2 uyarı P-30b'nin dosyasında — § 4) |
| 2 | Tam süit 0 başarısız **ve test sayısı düşmeyecek** | **KISMEN** | Sayı **4035 → 4042** (düşmedi, test silinmedi) · kalan tek kırmızı **P-30b'nin** yarım işi, B-1'e ait 15 kırmızının **15'i kapandı** |
| 3 | `bash dotnet/denetim.sh` TEMİZ | **KARŞILANDI** | `DENETİM TEMİZ.` exit=0 (devirdeki ihlal B-1 tarafından kapatıldı) |
| 4 | Mevcut uç kaldırılmadı | **KARŞILANDI** | Envanter kıyası: **silinen uç 0** · eklenen 3 (`GET`+`PATCH /kullanici/gorunum`, `PATCH /yedekler/talep/{id}/gerekce` — sonuncusu başka şeridin) · `[Http` özniteliği toplam **367** |
| 5 | Mevcut DOM id'leri değişmedi | **KARŞILANDI** | `ENVANTER-S0.json` ↔ taze envanter: `sayfa_eylemleri` **silinen 0**, eklenen 12 · `bilesen_dom_id` silinen 0 |
| 6 | Özellik kaybı 0 | **KARŞILANDI** | `envanter-uret.sh` yeniden koştu: rota/menü/basit-mod/POS türü **silinen 0**; yalnız ekleme var |
| + | Para kapıları | **DOKUNULMADI** | B-1 para el değiştiren hiçbir yol açmıyor; `Hareketler.Add(` yine TEK yerde (`HareketFabrikasi.cs`) |

## 10. Değişen dosyalar (bu oturum, B-1 eliyle)

```
M  dotnet/Kuyumcu.Web/Pages/Ayarlar.razor          (P-11 düzeltmesi: başarı bandı temizliği try'dan önceye)
M  dotnet/Kuyumcu.Tests/UX1C4EkranTestleri.cs      (DI kaydı + gerçekçi sahte yanıt)
M  dotnet/Kuyumcu.Tests/SurumEkranTestleri.cs      (DI kaydı + gerçekçi sahte yanıt)
M  dotnet/Kuyumcu.Tests/TemaEkranTestleri.cs       (DI kaydı + yol farkında sahte API)
?? dotnet/Kuyumcu.Tests/B1SahteGorunumYaniti.cs    (YENİ — sahte /kullanici/gorunum gövdesi tek yerde)
A  dokumanlar/09-uygulama-kayitlari/PAKET-B-1-uygulama-raporu.md (bu dosya)
```
Devralınan (önceki ajan, aynı şerit): `Kuyumcu.Api/Modules/Gorunum/*`, `Kuyumcu.Api/Data/Migrations/20260923135612_*`,
`Kuyumcu.Api/Data/Entities.cs`, `Kuyumcu.Contracts/Sayfalar/GorunumSozlesmeleri.cs`,
`Kuyumcu.Web/Servisler/{Gorunum,KullaniciGorunum}Servisi.cs`, `Kuyumcu.Core/I18n/GorunumSozlugu.cs`,
`Kuyumcu.Tests/{B1GorunumEkranTestleri,B1KullaniciGorunumTestleri}.cs`.
**Dokunulmayanlar:** `MIMARI-KALAN-PAKETLER-2026-09-15.md`, `PLAN-SADELESTIRME-PUSULA-2026-09-23.md`,
`SONRAKI-OTURUM-PROMPT.md`, `Sozlukler.cs`, `belirtecler.css`/`app.css`/`Layout/*` (S-1), `denetim.sh`.

## 11. Dokunulan sayfalar (TEK DOKUNUŞ kaydı — DoD EK 2)

- `Kuyumcu.Web/Pages/Ayarlar.razor` — **ikinci dokunuş, gerekçeli:** aynı dalgada B-1'in kendi kartı yazılmıştı;
  bu oturumdaki dokunuş **kapı düzeltmesidir** (denetim.sh P-11 sayacı), yeni DOM/özellik eklemez.

## 12. Açık kalanlar

1. **P-30b'nin kırmızısı** `OlcumKapisiTestleri.denetim_sh_HER_CAGRI_DIZININDEN_AYNI_KARARI_VERIR` ve aynı dosyadaki
   2 `CS8604` uyarısı — **B-1'e ait değil**, DALGA 7c kapatacak.
2. **S-1 şeridi** `belirtecler.css`'teki 8 blok + `data-yazi` kademelerini sunucunun kanonik slug'larıyla
   (`platin`, `16/18/20`, `sistem/aydinlik/karanlik`) tam eşleştirmeli; sunucu tarafı hazır ve beklemede.
3. **`_Host`/`App` render'ında `html` etiketine `data-tema`/`data-sema`/`data-yazi`'nın SUNUCUDAN basılması (FOUC)** —
   TEMA-SİSTEMİ § 9'un bu maddesi B-1 kapsamında **doğrulanmadı**; S-1 yerleşim şeridinin işidir (`Layout/*` izolasyonu).

## 13. Kurucuya / mali müşavire notlar

- **Mali müşavir notu YOK:** bu paket hiçbir vergi/MASAK/tevsik/KDV hükmü içermez; renk ve punto tercihidir.
- **Kurucuya, tek bilinçli görsel kırılma:** varsayılan gövde puntosu **18 px**'tir (eski 14 px değil).
  TEMA-SİSTEMİ § 7 "16" diyordu; onaylanan tuval ve canlı CSS "18" diyor — **tuval kazandı** (§ 2, S-1).
  Farklı isteniyorsa tek satır (`GorunumSecenekleri.OlcekVarsayilan`) ve CSS `--yazi-govde` birlikte değişir.
- **Kurucuya:** "her kullanıcı kendi temasını seçer" artık sunucuda kalıcıdır; **dükkân varsayılanı hâlâ yalnız SAHİP'tedir**.
- **Kâğıt daima beyaz** kırmızı çizgisi testle kilitlidir (`B1_4`) ve mutasyonla kanıtlanmıştır (M-B1-4).
