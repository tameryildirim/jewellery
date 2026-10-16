# PAKET UX-3 — Basit / Profesyonel mod · **FAZ A** uygulama raporu

> Tarih: 2026-09-16 · Uygulayıcı: Taşkın (backend) + Ertuğ (frontend) rolü, Opus · Dalga 1
> Kapsam: **YALNIZ Faz A.** Faz B (POS basit akışı) ve Faz C (sayfa adaptasyonları) BU DALGADA YAPILMADI.
> Durum: **BİTTİ** — derleme 0 hata, 1142/1142 test yeşil, `./denetim.sh` TEMİZ, canlı doğrulama kanıtlı, test kiracısı silindi.
> COMMIT YOK — değişiklikler çalışma ağacındadır.

---

## 1. Yapılanlar

### 1.1 Veri modeli + migration
| Tablo | Kolon | Tip | Not |
|---|---|---|---|
| `kullanicilar` | `arayuz_modu` | `varchar(12)` **NULL** | NULL = "bilinmiyor" DEĞİL, **"kiracı varsayılanını devral"**. XML yorumunda açıkça yazılı. |
| `tenants` | `varsayilan_arayuz_modu` | `varchar(12)` **NOT NULL DEFAULT 'PROFESYONEL'** | Geri-doldurma gerekmez: bugünkü davranış zaten profesyoneldir. |

Migration: `dotnet/Kuyumcu.Api/Data/Migrations/20260916072554_ArayuzModu.cs` (+ `.Designer.cs`, 93.994 bayt).
Üretim **build YEŞİLKEN** yapıldı, `--no-build` hiç kullanılmadı.

`kuyumcu_net`'e uygulandı (`dotnet ef database update` → `Applying migration '20260916072554_ArayuzModu'. Done.`), SQL kanıtı:

```
mysql> show columns from kullanicilar like 'arayuz_modu';
arayuz_modu             varchar(12)  YES  NULL
mysql> show columns from tenants like 'varsayilan_arayuz_modu';
varsayilan_arayuz_modu  varchar(12)  NO   PROFESYONEL
mysql> select count(*), sum(arayuz_modu is null) from kullanicilar;
11   11
mysql> select varsayilan_arayuz_modu, count(*) from tenants group by 1;
PROFESYONEL   5
```
→ **Mevcut 11 kullanıcının ve 5 kiracının davranışı değişmedi.**

⚠ İlk üretilen migration `defaultValue: ""` yazmıştı (EF, CLR başlatıcısını görmez). Migration `remove` edilip
`KuyumcuDbContext.OnModelCreating` içine `HasDefaultValue("PROFESYONEL")` eklendi ve **yeniden üretildi**;
böylece migration, Designer ve ModelSnapshot birbiriyle tutarlı. Elle düzeltilmiş migration bırakılmadı.

### 1.2 Sunucu
- **YENİ** `Modules/Kullanici/ArayuzModu.cs` — tek doğruluk kaynağı: `Coz(kullaniciModu, kiraciVarsayilani)`, `Gecerli(mod)`.
  Bilinmeyen/bozuk değer **PROFESYONEL**'e düşer (sade yüze DÜŞMEZ — gizlenen alanın hiç doldurulmaması riski).
- `Data/Entities.cs` — `Sabitler.ArayuzModlari = ["BASIT","PROFESYONEL"]`, `Kullanici.ArayuzModu`, `Tenant.VarsayilanArayuzModu`.
- `Modules/Kullanici/KullaniciModulu.cs`
  - `KullaniciGuncelleDto.ArayuzModu` + **`ArayuzModuGonderildi` [JsonIgnore] bayrağı** — mevcut `SubeIdGonderildi` deseninin birebir kopyası (Mehmet K4). JsonElement/Optional&lt;T&gt; gibi **yeni bir desen açılmadı**.
  - `KullaniciOlusturDto.ArayuzModu` (Bertan K8) — boş/null = devral.
  - Üçlü durum **tek yardımcıda**: `ArayuzModuAyarla` (Bertan K5). `ModDogrula` beyaz listeyi servis içinde de uygular (DataAnnotations yalnız controller yolunda koşar).
  - `Temizle` yanıtına `arayuzModu`.
  - Yeni alan `HedefUzerindekiYetkiKontrol` çağrısının **ARKASINA** yazıldı (Mehmet K5); rol değişimi hâlâ ayrı uçtadır.
- `Modules/Auth/AuthModulu.cs` — `/auth/ben` ve giriş yanıtı `arayuzModu` + **`etkinArayuzModu`** döner; çözüm `ArayuzModu.Coz` ile **yalnız sunucuda** yapılır. `AktifKullanici.FromClaims`e alan **eklenmedi**, JWT claim'ine **konmadı** (Mehmet K6).
- `Modules/Admin/AdminModulu.cs` — **PATCH `/api/v1/admin/tenant`** açıldı; **ayrı DTO** (`TenantArayuzModuDto`, tek alan), toplu atama yok, rol kapısı **sınıf düzeyinde** `[RolGerekli("SAHIP")]`.
- `Modules/Kur/KurModulu.cs` — **Mehmet K3**: `PATCH /kur/kaynak` artık `[RolGerekli("SAHIP","MUDUR")]` (manuel kur ucuyla aynı hizada).

### 1.3 İstemci
- **YENİ** `Servisler/ArayuzModuServisi.cs` — `EtkinMod`, `KullaniciModu`, `BasitMi/ProfesyonelMi`, `MenudeGorunur(yol)`, `Degisti`, `DegistirAsync(mod)`.
  **Çözüm kuralını TEKRARLAMAZ** (değer `/auth/ben`den gelir) ve **localStorage'a YAZMAZ** (Bertan K6): mod bir *hesap* tercihidir, dil/tema gibi *cihaz* tercihi değildir.
  Kurucusunda **HttpClient/ApiIstemci YOK** (Aşkın K2) — bUnit sınıfları tek satırla kaydeder; PATCH gerekince `ApiIstemci` kapsamdan o an çözülür.
- **YENİ** `Servisler/BasitMenu.cs` — basit menü listesi **TEK YERDE** (7 rota). Razor'a dağılmış `@if (basit)` koşulu yazılmadı (Bertan K7).
- **YENİ** `Bilesenler/ProAlan.razor` — `ChildContent` / `BasitAlternatif` / `ZorunluGorunur`. Yalnız **çizim** kararı; çocuklarının id'lerine dokunmaz; gövdeye karışmaz.
- `Servisler/OturumServisi.cs` — `OturumKullanici` record'una `string? ArayuzModu = null, string EtkinArayuzModu = "PROFESYONEL"` **SONA ve VARSAYILANLI** (Aşkın K2) → mevcut 6 argümanlı çağrıların hepsi derlenmeye devam etti.
- `Layout/AnaYerlesim.razor` — yeni alanlar `TryGetProperty` + `ValueKind` denetimiyle okunur; **alan yoksa** (eski sunucu) PROFESYONEL'e düşer, çökmez.
- `Layout/UstBar.razor` — `#ust-mod-anahtar` > `#ust-mod-basit` / `#ust-mod-profesyonel` (DilSecici pill deseni). Dar ekranda **gizlenmez**.
- `Layout/KenarCubugu.razor` — her bağlantıda **iki koşul**: `Yetki.Erisebilir(modul) && Modu.MenudeGorunur(yol)`. `YetkiServisi.Erisebilir` **değişmedi**.
- `Pages/Ayarlar.razor` — `#ayar-arayuz-modu` kartı, üçlü seçici `#arayuz-devral` / `#arayuz-basit` / `#arayuz-profesyonel`.
- `Pages/Kullanicilar.razor` — düzenle modalında `#d-arayuz-modu`, yeni kullanıcı modalında `#k-arayuz-modu`. Boş seçenek gövdeye **açık null** gönderir.
- `Pages/Isletme.razor` — `#i-varsayilan-arayuz-modu` → PATCH `/admin/tenant`.
- `Kuyumcu.Core/I18n/Sozlukler.cs` — `SozlukArayuz` **TR + EN** (14 anahtar; `AdimUrun/AdimMusteri/AdimOdeme/AdimFis` ve `YasalKapiAcildi` Faz B için hazır). Razor'a çıplak metin yazılmadı.
- `wwwroot/css/app.css` — `.mod-anahtar` (yalnız `var()` belirteçleri, sabit renk yok, satır içi stil yok).
- `Program.cs` — `AddScoped<ArayuzModuServisi>()`.

### 1.4 denetim.sh (Mehmet K1 · Aşkın K5)
`dotnet/denetim.sh` "Güvenlik yüzeyi" bloğuna **iki sayaç** eklendi (`Data/Migrations/` `kod_say` tarafından zaten elenir):
1. `ArayuzModu (yalnız 6 dosya)` — beklenen **6**.
2. `ArayuzModu kapı yüzeyinde (0 olmalı)` — `Auth/` + `Modules/{Tevsik,Masak,Satis,Urun,Yetki,Kasa,Cari,Emanet,Hurda,Fiyat,Sayim,Rapor,Maliyet}/` altında **0**.

Beklenen ihlal çıktıları betiğin yorumlarına yazıldı.

---

## 2. Spesifikasyondan sapmalar ve gerekçeleri

| # | Sapma | Gerekçe |
|---|---|---|
| S1 | Üçlü durum için **JsonElement/Optional&lt;T&gt; KULLANILMADI**; mevcut `XGonderildi` setter bayrağı deseni kopyalandı. | Spesifikasyon "JsonElement veya Optional&lt;T&gt; gerekir" diyordu; Mehmet K4 ve Aşkın K6 bunu **yasakladı** (yeni desen açılmaz). Görev metni de bunu bağlayıcı kıldı. Üç durum tek yardımcıda toplandı. |
| S2 | denetim sayacı **satır sayısı değil DOSYA sayısı** ve beklenen **4 değil 6**. | (a) Satır sayacı her yorum/biçim düzenlemesinde kırılırdı — yanlış alarm güvenlik sayacını işe yaramaz hâle getirir. (b) 6 dosya: spesifikasyonun saydığı 4 + `Data/KuyumcuDbContext.cs` (NOT NULL varsayılanın model yapılandırması) + `Modules/Admin/AdminModulu.cs` (PATCH /admin/tenant — spesifikasyonun kendi "dosya ayak izi" listesinde de var). M-UX3-07 mutasyonu **her iki sayacı da** kırmızıya çevirdi (kanıt §5). |
| S3 | **PATCH /admin/tenant** yeni açıldı (spesifikasyon "UX-1'de açılan uç genişletilir" diyordu). | UX-1 Faz B sevk edilmemiş; kodda yalnız `GET /admin/tenant` vardı. Görev metni "PATCH /admin/tenant (ayrı DTO, toplu atama yok)" dedi; `/admin/arayuz-modu` diye ikinci bir uç açmak yerine sözleşmeye uyuldu. ⚠ UX-1 Faz B geldiğinde **aynı uca alan eklemesin**, ayrı DTO deseni korunsun. |
| S4 | Basit menüde **7 rota** var ve `/urunler/etiketler` dâhil. | Spesifikasyon metni kendi içinde tutarsızdı ("7 bağlantı" diyip 6 modül sayıyor; test maddesi "7 modül" diyor). Ana belgenin **sayfa envanteri** "Ürün Etiketleri → Görünür (barkod/etiket basmak perakendede günlük iş)" diyor; envanter esas alındı. Liste tek yerde (`BasitMenu.Yollar`) olduğu için kurucu kararı tek satırla değişir. |
| S5 | `Coz` bilinmeyen değerde **PROFESYONEL**'e düşer (spesifikasyon bu ucu tanımlamamıştı). | Fail-safe yönü: bilinmeyende alan **gizlemek**, o alanın hiç doldurulmaması riskini doğurur (Anayasa md.1 — varsayım yapılmaz). Testle sabitlendi. |
| S6 | `/auth/giris` yanıtı da `arayuzModu` + `etkinArayuzModu` taşır. | `Temizle` tek metottur; iki ayrı yanıt biçimi üretmemek için giriş yolunda da çözüm yapıldı. İstemci yine yalnız `/auth/ben`i okur. |

**Mevzuat/DOGRULANMALI hükmü yok:** bu paketin "Mevzuat dayanağı" bölümü `(yok)`. Basit mod hiçbir eşik/oran/had sayısı içermez; ekrana sabit eşik yazılmadı (Mehmet K2 zaten Faz B'nin konusu).

---

## 3. Testler

| Koşum | Kapsam | Toplam | Başarılı | Başarısız | Atlanan |
|---|---|---|---|---|---|
| Filtreli 1 | `ArayuzModuCoz` + `ProAlanEkran` + `BasitModMenu` | 20 | 20 | 0 | 0 |
| Filtreli 2 | `ArayuzModuVeritabani` + `HttpArayuzModu` | 15 | 15 | 0 | 0 |
| **Tam süit** | `Kuyumcu.Tests` (trx) | **1142** | **1142** | **0** | **0** |

- **Yeni test sayısı: 35** (trx'ten sınıf sınıf sayıldı: ArayuzModuCoz 5 · ArayuzModuVeritabani 5 · HttpArayuzModu 10 · ProAlanEkran 7 · BasitModMenu 8).
- Önce/sonra: **1107 → 1142**. ⚠ Dürüstlük notu: 1107 rakamı *ölçülmedi*, 1142 − 35 ile hesaplandı; bu oturumda değişiklik ÖNCESİ tam süit koşulmadı. Kırmızı yok.
- trx: `…/scratchpad/trx-pUX-3/_Ramazan-MacBook-Air_2026-09-16_10_46_37.trx` → `<Counters total="1142" … passed="1142" failed="0" …/>`
- **Mevcut testlere dokunulan satır sayısı: 2** (Aşkın K2'nin istediği rapor): `RotaBaslikTestleri.cs` ve `TemaEkranTestleri.cs` içine birer `Services.AddScoped<ArayuzModuServisi>();`. **Hiçbir assert değişmedi.**

Yeni dosyalar:
- `dotnet/Kuyumcu.Tests/ArayuzModuTestleri.cs` (saf `Coz` · DB davranışı · HTTP üçlü durum + rol kapıları)
- `dotnet/Kuyumcu.Tests/ProAlanEkranTestleri.cs`
- `dotnet/Kuyumcu.Tests/BasitModMenuTestleri.cs` (**KenarCubugu bu projede ilk kez çiziliyor** — sahte `/yetkiler/benim` + OturumServisi + ArayuzModuServisi)

⚠ Servis testlerinde `/ben` yolu kullanıldı: `Guncelle` yolunda `HedefUzerindekiYetkiKontrol` SAHİP'in SAHİP üzerinde işlem yapmasını engelliyor (kapı çalışıyor, test ona uyarlandı — kapı gevşetilmedi).

---

## 4. Derleme

```
dotnet build Kuyumcu.sln --nologo
Oluşturma başarılı oldu.
    0 Uyarı
    0 Hata
```
⚠ Test projesi **yeniden derlendiğinde** 1 uyarı çıkıyor: `RotaBaslikTestleri.cs(155): warning xUnit1031` — **bu paketten ÖNCE de vardı**, benim dokunmadığım satır (`.Result` kullanımı). Gizlenmemesi için yazıldı; düzeltmesi izin ister (Kural 3).

---

## 5. Mutasyon kanıtları

| Kod | Değişiklik | Kırmızıya dönen testler | Geri alım (`cmp`) |
|---|---|---|---|
| **M-UX3-02** | `ArayuzModu.Coz`: `if (Gecerli(kullaniciModu)) return kullaniciModu!;` → `if (!Gecerli(kullaniciModu)) return Basit;` (null kullanıcı modu BASIT'e çözülür) | **7 kırmızı**: `ArayuzModuCozTestleri.Varsayilan_profesyoneldir_kullanici_tercihi_yoksa` · `.Kullanici_tercihi_kiraci_varsayilanini_EZER` · `.Bilinmeyen_deger_PROFESYONELE_duser_sade_yuze_DUSMEZ` · `ArayuzModuVeritabaniTestleri.Migration_sonrasi_MEVCUT_davranis_DEGISMEZ` · `HttpArayuzModuTestleri.Auth_ben_arayuzModu_ve_etkinArayuzModu_doner` · `.Mod_BASIT_yapilinca_auth_ben_BASIT_doner` · `.Mod_degisimi_YENIDEN_GIRIS_gerektirmez_claimde_tasinmaz` | `cmp` → AYNI |
| **M-UX3-03** | `KullaniciModulu.ArayuzModuAyarla`: "gövdede yoksa koru" kaldırıldı → `if (!dto.ArayuzModuGonderildi) { hedef.ArayuzModu = null; return; }` | **3 kırmızı**: `ArayuzModuVeritabaniTestleri.Servis_govdede_alan_YOKSA_mevcut_degeri_KORUR` · `HttpArayuzModuTestleri.Ben_uc_durum_yok_null_deger` · `.Sahip_calisaninin_modunu_degistirir_uc_durum` | `cmp` → AYNI |
| **M-UX3-06** | `KenarCubugu.Gorunur`: `Yetki.Erisebilir(modul) && Modu.MenudeGorunur(yol)` → `||` (mod, yetkinin yerine geçer) | **5 kırmızı**: `BasitModMenuTestleri.Yetkisiz_kullanici_baglantiyi_BASIT_modda_da_gormez` · `.Yetkisiz_kullanici_baglantiyi_gormez_PROFESYONEL_modda` · `.Basit_modda_menu_YALNIZ_BasitMenu_listesidir` · `.Basit_modda_profesyonel_ekranlar_menude_YOKTUR` · `.Yetkili_TEZGAHTAR_basit_modda_calisma_ekranlarini_gorur` | `cmp` → AYNI |
| **M-UX3-07** | `Modules/Tevsik/TevsikKapisi.cs`'e `public static bool ModMuaf(string? ArayuzModu) => ArayuzModu == "BASIT";` eklendi (sunucu kapısına mod koşulu sızdırma) | **denetim.sh kırmızı**, gerçek çıktı:<br>`✗ ArayuzModu (yalnız 6 dosya)  7 (beklenen 6)`<br>`✗ ArayuzModu kapı yüzeyinde (0 olmalı)  1 (beklenen 0)`<br>`→ Modules/Tevsik/TevsikKapisi.cs:23: public static bool ModMuaf…` | `cmp` → AYNI |

M-UX3-01 · 04 · 05 · 08 **Faz B/C kapsamındadır** (POS akışı, gövde kurulumu, kasa seçici, tür sekmeleri) — bu dalgada uygulanmadı, dolayısıyla kurulmadı.

Son mutasyondan sonra **temiz `dotnet build`**: 0 hata / 0 uyarı; mutant ikili bırakılmadı. Yedekler: `…/scratchpad/mut-yedek/UX3/`.

---

## 6. denetim.sh çıktısı (son)

```
═══ Kiracı izolasyonu denetimi ═══
  ✓ IgnoreQueryFilters (yasak)                     0
  ✓ new KuyumcuDbContext (yalnız fabrika)          1
  ✓ Find/FindAsync (yasak)                         0
  ✓ ExecuteUpdate/Delete (beyaz liste 4)           4
  ✓ Hareketler.Add (yalnız HareketFabrikasi)       1
  ✓ VeresiyeKapanisi (yalnız Etiketten)            2
  ✓ IKiraciVarlik uygulayan varlık                31
  ✓ HasQueryFilter                                 2

═══ Güvenlik yüzeyi ═══
  ✓ oran sınırı                                  var
  ✓ HTTPS yönlendirme                             var
  ✓ sıkıştırma                                    var
  ✓ sağlık ucu                                    var
  ✓ ArayuzModu (yalnız 6 dosya)                    6
  ✓ ArayuzModu kapı yüzeyinde (0 olmalı)           0
  ✓ CORS                                           beyaz liste

DENETİM TEMİZ.
```

---

## 7. Canlı doğrulama (port 5198, gerçek veri)

Kiracı: **PAKETUX-3-TEST** (`01M2MJZGKRGS077M9FCZ949VDA`) · kullanıcılar: `ux3sahip@test.local` (SAHIP), `ux3tez@test.local` (TEZGAHTAR).

| # | Adım | Beklenen | Gerçek |
|---|---|---|---|
| 1 | `POST /auth/kayit` yeni kiracı | 201 | **201** |
| 2 | SQL: yeni kiracı + kullanıcı | `varsayilan_arayuz_modu='PROFESYONEL'`, `arayuz_modu` NULL | `01M2MJZ… PROFESYONEL` · `ux3sahip@test.local SAHIP <NULL>` |
| 3 | `GET /auth/ben` (SAHIP) | `arayuzModu:null`, `etkinArayuzModu:"PROFESYONEL"` | **aynen** (HTTP 200) — davranış değişmedi |
| 4 | `POST /kullanicilar` `arayuzModu:"BASIT"` | 201, `arayuzModu:"BASIT"` | **201**, `"arayuzModu":"BASIT"` (Bertan K8 kanıtı) |
| 5 | TEZGAHTAR girişi → `GET /auth/ben` | `BASIT` / `BASIT` | `"arayuzModu":"BASIT","etkinArayuzModu":"BASIT"` |
| 6 | `PATCH /kullanicilar/{id}` `{"ad":"Tezgah"}` | 200, **mod KORUNUR** | HTTP=200, `arayuzModu="BASIT"` |
| 7 | `PATCH /kullanicilar/{id}` `{"arayuzModu":null}` | 200, **DEVRAL** | HTTP=200, `arayuzModu=null` |
| 8 | `PATCH /kullanicilar/{id}` `{"arayuzModu":"basit"}` | 400, satır değişmez | **400** `{"message":["Geçersiz arayüz modu"],…}` |
| 9 | `PATCH /kullanicilar/{id}` `{"arayuzModu":"PROFESYONEL"}` | 200 | HTTP=200, `arayuzModu="PROFESYONEL"` |
| 10 | `PATCH /kullanicilar/ben` — üç durum + geçersiz | 200/200/200/400 | `BASIT` → (soyad) `BASIT` → null → **400** |
| 11 | TEZGAHTAR → `PATCH /kullanicilar/{id}` | 403 | **403** `Bu ekran için yetkiniz yok (kullanici)` |
| 12 | `PATCH /admin/tenant` `{"varsayilanArayuzModu":"BASIT"}` | 200 | HTTP=200, `varsayilanArayuzModu="BASIT"` |
| 13 | `PATCH /admin/tenant` `{}` / `{null}` / `{"basit"}` | 400 · 400 · 400 | **400 · 400 · 400** |
| 14 | TEZGAHTAR → `PATCH /admin/tenant` | 403 | **403** |
| 15 | Kiracı varsayılanı BASIT iken tercihsiz SAHİP | `etkinArayuzModu:"BASIT"`, kolon hâlâ NULL | `"arayuzModu":null "etkinArayuzModu":"BASIT"` · SQL: `ux3sahip@test.local <NULL>` |
| 16 | Tercihi olan TEZGAHTAR aynı anda | **etkilenmez** (toplu atama yok) | `"arayuzModu":"PROFESYONEL" "etkinArayuzModu":"PROFESYONEL"` · SQL: `ux3tez@test.local PROFESYONEL` |
| 17 | **Mehmet K3** TEZGAHTAR → `PATCH /kur/kaynak` | 403 | **403** `Bu işlem için gerekli rol: SAHIP veya MUDUR` |
| 18 | SAHİP → `PATCH /kur/kaynak` | 200 | **200** `{"secili":"TRUNCGIL","uyari":null}` |
| 19 | API durdurma | 5198 boş | `lsof -nP -iTCP:5198 -sTCP:LISTEN` → **çıktı yok** |
| 20 | Test kiracısı silme (31 `tenant_id` tablosu + `tenants`) | kiracı sayısı 5'e döner | **5** · kiracıya ait kullanıcı 0 · tüm kullanıcılar `arayuz_modu` NULL (11/11) · tüm kiracılar PROFESYONEL (5) |

**Kiracı sayısı: başlangıç 5 → son 5.**

---

## 8. Değişen dosyalar

**YENİ**
```
dotnet/Kuyumcu.Api/Modules/Kullanici/ArayuzModu.cs
dotnet/Kuyumcu.Api/Data/Migrations/20260916072554_ArayuzModu.cs
dotnet/Kuyumcu.Api/Data/Migrations/20260916072554_ArayuzModu.Designer.cs
dotnet/Kuyumcu.Web/Servisler/ArayuzModuServisi.cs
dotnet/Kuyumcu.Web/Servisler/BasitMenu.cs
dotnet/Kuyumcu.Web/Bilesenler/ProAlan.razor
dotnet/Kuyumcu.Tests/ArayuzModuTestleri.cs
dotnet/Kuyumcu.Tests/ProAlanEkranTestleri.cs
dotnet/Kuyumcu.Tests/BasitModMenuTestleri.cs
```
**DEĞİŞİKLİK**
```
dotnet/Kuyumcu.Api/Data/Entities.cs
dotnet/Kuyumcu.Api/Data/KuyumcuDbContext.cs
dotnet/Kuyumcu.Api/Data/Migrations/KuyumcuDbContextModelSnapshot.cs
dotnet/Kuyumcu.Api/Modules/Kullanici/KullaniciModulu.cs
dotnet/Kuyumcu.Api/Modules/Auth/AuthModulu.cs
dotnet/Kuyumcu.Api/Modules/Admin/AdminModulu.cs
dotnet/Kuyumcu.Api/Modules/Kur/KurModulu.cs
dotnet/Kuyumcu.Core/I18n/Sozlukler.cs
dotnet/Kuyumcu.Web/Servisler/OturumServisi.cs
dotnet/Kuyumcu.Web/Layout/AnaYerlesim.razor
dotnet/Kuyumcu.Web/Layout/UstBar.razor
dotnet/Kuyumcu.Web/Layout/KenarCubugu.razor
dotnet/Kuyumcu.Web/Pages/Ayarlar.razor
dotnet/Kuyumcu.Web/Pages/Kullanicilar.razor
dotnet/Kuyumcu.Web/Pages/Isletme.razor
dotnet/Kuyumcu.Web/Program.cs
dotnet/Kuyumcu.Web/wwwroot/css/app.css
dotnet/denetim.sh
dotnet/Kuyumcu.Tests/RotaBaslikTestleri.cs        (1 satır DI)
dotnet/Kuyumcu.Tests/TemaEkranTestleri.cs         (1 satır DI)
```
⚠ `git status --porcelain` çıktısındaki diğer değişiklikler bu oturumdan ÖNCE vardı (depoda tek commit var, ağaç zaten kirli).

---

## 9. Koşul karşılama

| Koşul | Durum | Kanıt |
|---|---|---|
| Bertan K1 — Faz B, UX-2 Faz A1 bitmeden başlamaz | ✔ | Faz B **hiç yapılmadı**; `Islemler.razor`a dokunulmadı. |
| Bertan K2 — üç yasal kapı canlı kanıtı sevk kapısıdır | — (Faz B) | Faz B kapsamı; bu dalgada sevk edilmedi. |
| Bertan K3 — ProAlan içi alan gövdeye konmaz | ✔ (bileşen düzeyinde) | `ProAlan` gövdeye hiç karışmaz; sayfa uyarlaması Faz C. |
| Bertan K4 — Islemler.razor büyümesin | ✔ | Dosyaya **dokunulmadı** (satır sayısı aynı). |
| Bertan K5 — üç durumlu JSON tek yerde | ✔ | `ArayuzModuAyarla`; `/ben` ve `/{id}` aynı yardımcıyı kullanır; `/admin/tenant` NOT NULL olduğu için üçlü durum yoktur (null → 400, testli). |
| Bertan K6 — etkin mod yalnız sunucuda | ✔ | `/auth/ben` `etkinArayuzModu`; istemcide `Coz` yok; `TryGetProperty`; localStorage yazımı yok. |
| Bertan K7 — açık sorular kurucuya, karar tek yerden koda | ✔ | `BasitMenu.Yollar`; açık sorular §11. |
| Bertan K8 — POST /kullanicilar da kapsamda | ✔ | Canlı adım 4 + `Yeni_kullanici_olustururken_mod_verilebilir_ve_bos_DEVRALIR`. |
| Mehmet K1 — sunucu kapıları mod'dan habersiz + denetim sayacı | ✔ | İki sayaç; M-UX3-07 ikisini de kırdı. |
| Mehmet K2 — eşikler istemciye gömülmez | ✔ | Bu fazda ekrana hiçbir eşik sayısı yazılmadı. |
| Mehmet K3 — PATCH /kur/kaynak rol kapısı + 403 testi | ✔ | `[RolGerekli("SAHIP","MUDUR")]`; HTTP testi + canlı 403/200. |
| Mehmet K4 — mevcut `XGonderildi` deseni, regex | ✔ | `ArayuzModuGonderildi`; `[RegularExpression("BASIT\|PROFESYONEL")]`; `'basit'` → 400 (test + canlı). |
| Mehmet K5 — hiyerarşi kapısının arkasında, rolle aynı gövdede değil | ✔ | `ArayuzModuAyarla`, `HedefUzerindekiYetkiKontrol`ün sonrasında; rol ucu ayrı kaldı. |
| Mehmet K6 — claim'e konmaz | ✔ | `AktifKullanici` değişmedi; `Mod_degisimi_YENIDEN_GIRIS_gerektirmez_claimde_tasinmaz` (aynı token yeni modu görür). |
| Mehmet K7 — veri koruma | ✔ (bu fazda) | Bu fazda ProAlan'a alan alınmadı; `M-UX3-04` Faz C. |
| Mehmet K8 — menü daraltma yetkinin üstüne | ✔ | `&&`; `YetkiServisi.Erisebilir` değişmedi; M-UX3-06. |
| Mehmet K9/K10 — tek kasa, ZorunluGorunur çizim kararı | ✔ (K10) / — (K9, Faz B) | `ProAlan` yalnız çizer; kasa kuralı Faz B. |
| Aşkın K2 — mevcut testler derlenmeye devam eder | ✔ | Record alanları sonda + varsayılanlı; **yalnız 2 satır** DI eklendi, 0 assert değişti. |
| Aşkın K5 — denetim sayacı, Migrations hariç, beklenen çıktı betikte | ✔ | `kod_say` `/Migrations/` eler; beklenen ihlal satırları yorumda. |
| Aşkın K7 — `Coz` saf testi + migration sonrası DB testi | ✔ | 5 saf + `Migration_sonrasi_MEVCUT_davranis_DEGISMEZ`; M-UX3-02 ikisini de kırdı. |
| Aşkın K8 — KenarCubugu ilk testi, yetkisiz kullanıcı | ✔ | `BasitModMenuTestleri` (8 test), sahte `/yetkiler/benim`. |
| Görev — Faz B/C yapılmaz | ✔ | Islemler/Cariler/Urunler/Kasalar/Raporlar/Panel'e dokunulmadı. |
| Görev — mevcut element id'leri değişmez | ✔ | Yalnız **yeni** id'ler eklendi (`ust-mod-*`, `ayar-arayuz-modu`, `arayuz-*`, `d-arayuz-modu`, `k-arayuz-modu`, `i-varsayilan-arayuz-modu`). |
| Görev — yeni CSS'te sabit renk yok | ✔ | `.mod-anahtar` yalnız `var()`. |
| Görev — Sozlukler TR+EN | ✔ | `SozlukArayuz` iki sözlükte de dolu. |
| Görev — COMMIT YOK | ✔ | `git` yazma komutu çalıştırılmadı. |

---

## 10. Açık kalanlar

1. **Faz B (POS basit akışı)** ve **Faz C (sayfa adaptasyonları)** yapılmadı; `M-UX3-01/04/05/08` mutasyonları ve `BasitModPosTestleri` / `BasitModVeriKorumaTestleri` o fazlara aittir.
2. `ProAlan` **henüz hiçbir sayfada kullanılmıyor** (bileşen + testi hazır). İlk müşterisi Faz B/C olacak.
3. Basit modda sayfalar **içeriden değişmez**: menü daralır, sayfa içi bugünkü gibidir (Faz A sözü).
4. `RotaBaslikTestleri.cs:155` xUnit1031 uyarısı (bu paketten önce vardı) — düzeltmesi izin ister.
5. `Data/Migrations/` altında önceki oturumlardan kalan **0 baytlık 8 dosya** duruyor (silme kurucuya bırakıldı; hook `rm` bloklar).
6. UX-1 Faz B geldiğinde `PATCH /admin/tenant` **ayrı DTO** deseni korunmalı (toplu atama açılmasın).

---

## 11. Kurucuya / mali müşavire notlar

**Mali müşavire gerek YOK:** bu paket hiçbir mevzuat hükmü uygulamıyor, hiçbir eşik/oran/had içermiyor. Basit mod **yalnız görünürlük ve varsayılan** belirler; tevsik, MASAK, taş kökeni, gün kilidi, kredi limiti kapılarının hepsi sunucuda ve **mod'dan habersiz** çalışıyor (denetim.sh iki sayaçla zorluyor).

**Kurucu kararı bekleyen iş soruları** (mimari değil, iş kararı — Bertan K7):
1. Basit modda **emanet** görünmeli mi? (Bugün: gizli.)
2. Basit modda **fatura** kesilebilmeli mi? (Bugün: gizli.) Müşteri fatura isterse tezgâhtar ne yapacak?
3. Basit modda **veresiye** tamamen kapalı mı, yoksa vadesiz (aynı gün muaccel) veresiye açık mı?
4. **Yeni kiracılar** hangi modda başlasın? Bugün hepsi PROFESYONEL (geriye dönük uyum). Yeni kiracıya BASIT vermek daha iyi bir ilk izlenim olabilir; iki farklı varsayılan karmaşıklık yaratır.
5. Basit menüde `/urunler/etiketler` kalsın mı? (Envantere göre evet; tek satırla değişir: `BasitMenu.Yollar`.)

Hangi cevap verilirse verilsin uygulama **tek yerden** yapılır: `dotnet/Kuyumcu.Web/Servisler/BasitMenu.cs`.
