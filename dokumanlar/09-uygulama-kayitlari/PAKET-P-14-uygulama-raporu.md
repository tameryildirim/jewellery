# PAKET P-14 — MENÜ / BİLGİ MİMARİSİ + ETİKET ÇARPIŞMALARI

**Dalga:** DALGA 4 · **Kimlikler:** Y-9 (+ P-00 triyaj temaları B-59 · B-63 · B-65 · B-68)
**Sürüm:** 1.0 · **Tahmin:** 120 ajan-dk · **Migration:** HAYIR (üretilmedi, gerekmedi)
**Tarih:** 2026-09-21 · **Rol:** Taşkın (backend) + Ertuğ (frontend)

---

## 0. ÖNKOŞUL ÖLÇÜMÜ (P-01) — durma-devam et modunda

| Ölçüm | Sonuç |
|---|---|
| `dokumanlar/09-uygulama-kayitlari/PAKET-P-01-uygulama-raporu.md` | **VAR** (diskte) |
| `denetim.sh` içinde `# >>> P-01` işaretli sayaç bloğu | **VAR** (satır 1037-1090) |
| `denetim.sh` seri sırası P-01 ile başlıyor mu (BK-08) | **EVET** |

Önkoşul karşılandı; kapsam daraltılmadı.

---

## 1. SPESİFİKASYONUN KODA KARŞI DOĞRULANMASI

| Plan iddiası (satır 389-398) | Kodda ölçülen (2026-09-21) | Sonuç |
|---|---|---|
| `Layout/KenarCubugu.razor` (`Paylasilan` değil) | `dotnet/Kuyumcu.Web/Layout/KenarCubugu.razor` | **DOĞRU** |
| 38 href | `grep -c 'href="/'` → **38** | **DOĞRU** |
| 0 grup başlığı | başlık öğesi yok | **DOĞRU** |
| `Sozlukler.cs:2770,2777` (4 etiket çarpışması) | satırlar kaymış: TR Nav bloğu 2876+; çarpışan dört çift **aynen duruyor** | **DOĞRU (satır no kaydı)** |
| `Layout/RotaBasliklari.cs` | var, 42 rotanın tamamını taşıyor | **DOĞRU** |

**Sapma 1 — `RotaBasliklari.cs` DEĞİŞTİRİLMEDİ.** Rota kimliği hiç değişmedi (hiçbir `@page`
eklenmedi/silinmedi/yeniden adlandırılmadı); başlıklar zaten `t.Nav.*`'tan geliyor, dolayısıyla
etiket düzeltmeleri üst bara kendiliğinden yansıdı. Dosyaya gereksiz dokunmak "TEK DOKUNUŞ"
kuralına aykırı olurdu.

**Sapma 2 — YENİ DOSYA: `Layout/MenuTanimi.cs`.** Plan "Dosyalar" listesinde yok. Gerekçe:
PLAN § 1 md.1 **MEKANİZMA ÖNCE** — üç sayaç (`MENU_AYNI_AD`, `MENU_KAPSAM`,
`BASIT_MOD_TUTARSIZ`) ancak menü **veri** olduğunda ölçülebilir. 38 bağlantı razor içinde
tek tek `@if` bloğuyla dururken "aynı ad iki hedefe mi gidiyor" sorusu ne testle ne betikle
sorulabiliyordu. Dosya plandaki `Layout/` ayak izinin içindedir ve razor'dan kod taşır,
kapsam açmaz. (Plan bloğu ile bu koşullar çelişmedi; çelişki yok.)

**Sapma 3 — Sözlükte plan bloğunun saymadığı üç ek hizalama.** Etiket çarpışmaları
düzeltilince `RaporSeti.AdIslemDefteri` / `AdKasaDefteri` / `AdVadeTakibi`, `Vade.Baslik` ve
`Pos.TahsilatBaslik` **menüden farklı ad** taşır hâle geliyordu (menüde "Hareket Dökümü",
sayfa başlığında "İşlem Defteri"). Bu, UX-S31'in birebir ihlali olurdu; hepsi tek ada
hizalandı. Aynı dosya (`Sozlukler.cs`), kapsam büyümesi yok.

**Mevzuat/DOGRULANMALI hükmü:** bu pakette yok — menü ve etiket işidir.

---

## 2. YAPILANLAR

### 2.1 Gruplama (38 bağlantı → 6 grup)
`Layout/MenuTanimi.cs` (YENİ) menüyü veri olarak tanımlar: `MenuOgesi(Grup, Yol, Modul,
Roller, Ikon, Etiket, TamEslesme)`. 36 kenar çubuğu bağlantısı + 2 alt blok = 38.

| Grup (slug) | TR / EN başlık | Bağlantı |
|---|---|---|
| `gunluk` | Günlük İş / Daily Work | /, /islemler, /emanet, /siparisler, /bildirimler |
| `musteri-stok` | Müşteri ve Stok / Customers and Stock | /cariler, /urunler, /urunler/agac, /urunler/etiketler, /fiyatlandirma, /sayim |
| `para-banka` | Para ve Banka / Money and Bank | /kasalar, /vezne, /pos, /pos/tahsilatlar, /banka, /evraklar, /evraklar/takvim, /kur |
| `belge-uyum` | Belge ve Uyum / Documents and Compliance | /faturalar, /gider-pusulalari, /uyum |
| `rapor` | Dökümler / Listings | /raporlar, /raporlar/kar-zarar, /raporlar/vade-takibi, /raporlar/islem-defteri, /raporlar/satis-analizi, /raporlar/kasa |
| `yonetim` | Yönetim / Administration | /kullanicilar, /isletme, /yetkiler, /denetim-izi, /kurulum, /veri-disa-aktarim, /veri-ice-aktarim, /yedekler |

`KenarCubugu.razor` artık yalnız çizer. **Boş grubun başlığı çizilmez** (basit modda ya da
dar yetkide altı boş başlık kalmaz). Yetki/rol/mod koşulu **aynen korundu**: `Gorunur(oge)`
= (modül yetkisi VE rol) **VE** `Modu.MenudeGorunur` — M-UX3-06 kuralı gevşetilmedi.

### 2.2 Dört etiket çarpışması (TR + EN, tek ad kuralıyla)

| # | Eski | Yeni (TR) | Yeni (EN) | Hizalanan diğer anahtarlar |
|---|---|---|---|---|
| 1 | İşlemler ↔ **İşlem Defteri** | **Hareket Dökümü** | Movement Listing | `RaporSeti.AdIslemDefteri` |
| 2 | Kasalar ↔ **Kasa Defteri** | **Kasa Hareketleri** | Cash Movements | `RaporSeti.AdKasaDefteri` |
| 3 | Vade Takvimi ↔ **Vade Takibi** | **Gecikmiş Alacaklar** | Overdue Receivables | `RaporSeti.AdVadeTakibi`, `Vade.Baslik` |
| 4 | POS Cihazları ↔ **POS Alacakları** | **POS Hakedişleri** | POS Settlements | `Pos.TahsilatBaslik` ("POS Alacak Defteri" idi) |

### 2.3 Basit mod tutarlılığı
`BasitMenu.Yollar` (8 yol) DEĞİŞMEDİ — ölçüm zaten tutarlıydı. Yeni olan, tutarlılığın
**mekanik kilidi**: basit menüdeki her yol menü tanımında bulunmalı ve üst bardaki rozetin
hedefi (`/bildirimler`) basit menüde olmalı; ikisi de hem test hem `denetim.sh` ile bağlandı.

### 2.4 Erişilebilirlik (B-63, kendi yüzeyimde)
- Grup başlıkları `<h2 class="kenar-grup-baslik">` (h1→h3 atlaması yapılmadı).
- `<nav class="kenar-nav" aria-label="@Dil.T.Nav.MenuEtiketi">` — yeni sözlük anahtarı
  `Nav.MenuEtiketi` = "Ana menü" / "Main menu" (isimsiz landmark borcu kapandı).
- CSS: `.kenar-grup-baslik` yalnız belirteç (`var(--kenar-metin)`, `var(--radius-*)` komşuluğu);
  **sabit renk yok, satır içi style yok.**

---

## 3. DoD SÖZLEŞMESİ (Bertan koşulu)

Bu paket **hiçbir yeni varlık / kayıt türü yaratmaz**: migration yok, tablo yok, uç yok,
DTO yok. PATCH/DELETE/`[ModulYetkisi]`/denetim kaydı yükümlülüğü **doğmaz**.

| Nesne | Düzenlenebilir mi | Nasıl / neden |
|---|---|---|
| Menü tanımı (`MenuTanimi.Ogeler`) | **Kod sabiti — salt okunur (çalışma zamanı)** | Menü bir kullanıcı kaydı değil, ürün bilgi mimarisidir. Kullanıcıya özel menü = ayrı paket (kapsam dışı). Değişimi tek dosyada, tek satırda yapılır; test + sayaç bozulmayı yakalar. |
| Grup başlıkları / etiketler | **Sözlükten** (`Sozlukler.cs` TR+EN) | Çıplak metin yasağı korunur; eksik anahtar = derleme hatası. |
| Basit mod listesi (`BasitMenu.Yollar`) | **Kod sabiti — tek kaynak** | UX-3 mührü: razor'a dağılmış `@if (basit)` yasak. |
| Kullanıcının arayüz modu (BASIT/PROFESYONEL) | **Evet — PATCH `/kullanicilar/ben`** | Mevcut `ArayuzModuServisi.DegistirAsync` (UX-3 paketi). Bu paket dokunmadı. |

Finansal varlık yok → hard DELETE sorusu doğmuyor.
"Sunucuda uç var, ekranda düğme yok" (UX-S25) yasağının menü karşılığı `MENU_KAPSAM`
sayacıyla mekanikleşti: gerekçesiz `@page` = kapı kırılır.

**Her düğme gerçek istek atar (DoD EK md.1):** bu pakette düğme eklenmedi/çıkarılmadı;
menüdeki 38 öğenin tamamı gerçek bir rotaya gider (canlı 42/42 = HTTP 200, § 7).

**TEK DOKUNUŞ (MÜHÜR 2) — dokunulan razor sayfaları:** `Layout/KenarCubugu.razor` (yalnız bu).
`Pages/` altında **hiçbir** sayfa açılmadı.

---

## 4. DERLEME

```
dotnet build Kuyumcu.sln --nologo   →   0 Uyarı · 0 Hata   (RC=0)
```

---

## 5. MIGRATION

**YOK.** Şema değişikliği gerekmedi; `dotnet ef` hiç çalıştırılmadı.
Kanıt: `__EFMigrationsHistory` = **55** (paket öncesi 55, sonrası 55 — § 7).

---

## 6. TESTLER

| Ölçüm | Önce | Sonra |
|---|---|---|
| Tam süit (`dotnet test Kuyumcu.Tests`) | 3204 geçti / 0 başarısız | **3222 geçti / 0 başarısız / 0 atlanan** |
| Yeni test | — | **18** (`Kuyumcu.Tests/MenuMimarisiTestleri.cs`) |
| Uyarlanan test | — | 2 (`IkonTestleri` menü sırası, `RaporEkranTestleri` sabit ad → sözlük) |

Ham çıktı: `…/scratchpad/tamsuit4.log` · TRX: `…/scratchpad/trx-pP-14/`

Yeni testler (18): gruplama (3) · MENU_AYNI_AD TR/EN + dört çift + eski ad cırcırı +
tek-ad hizalaması + `Sadelestir` birim testi (6) · MENU_KAPSAM (3) · BASIT_MOD_TUTARSIZ (2) ·
bUnit çizim (4).

**Uyarlanan testlerin gerekçesi (susturma DEĞİL):**
- `IkonTestleri.KenarCubugu_menu_rotalari_ikon_gecisinden_SONRA_da_ayni` — beklenen rota
  **sırası** grup sırasına göre yeniden yazıldı. Rota **kümesi birebir aynı** (27 rota, hiçbiri
  silinmedi/değişmedi); K4 "silinen/değişen = engel" kuralı rota kimliğine bakar.
- `RaporEkranTestleri.Rapor_Merkezi_katalogdan_kart_cizer` — sabit `"İşlem Defteri"` dizgesi
  `Sozlukler.Tr.RaporSeti.AdIslemDefteri`e çevrildi; assert **zayıflatılmadı**, kaynağı
  sözlüğe bağlandı.

---

## 7. MUTASYON KANITLARI

Yöntem: kaynak `…/scratchpad/mut-yedek/P-14/` altına kopyalandı → `perl -0777 -pi -e` ile tek
değişiklik → filtreli `dotnet test` → yedek geri kopyalandı → `cmp` ile birebir doğrulandı.

| Kod | Değişiklik | Kırmızıya dönen test(ler) | Sayaç tepkisi | Geri alım (`cmp`) |
|---|---|---|---|---|
| **M14-1** | `KenarCubugu.razor`: grup başlığı çizimi silindi | `Cizimde_grup_basligi_VARDIR_ve_altinda_en_az_bir_baglanti_bulunur` · `Basit_modda_BOS_grup_basligi_cizilmez` · `Yetkisiz_kullanicinin_grubu_HIC_cizilmez` (3) | `MENU_ELLE_BAGLANTI` ✗ (grup başlığı 0) | AYNI |
| **M14-2** | `Sozlukler.cs`: `IslemDefteri` → "İşlem Defteri" (çarpışma geri) | `MENU_AYNI_AD_sifir_TR_…` · `Dort_etiket_carpismasi_COZULDU` · `Eski_carpisan_adlar_GERI_GELMEZ` · `Ayni_kavram_TEK_ad_tasir_…` · `Cizilen_etiketler_menu_tanimiyla_AYNIDIR` (5) | `MENU_AYNI_AD` = **1** | AYNI |
| **M14-2b** | (taban ölçümü) dört adın **hepsi** eskiye döndürüldü | — | `MENU_AYNI_AD` = **4** → P-14 öncesi taban **taze ölçüldü**, belgeden kopyalanmadı | AYNI |
| **M14-3** | `MenuTanimi.cs`: `/sayim` satırı silindi | `MENU_KAPSAM_sifir_her_rota_ya_menude_ya_gerekceli` · `Menu_36_baglanti_tasir_ve_alt_blokla_38_eder` (2) | `MENU_KAPSAM` = **1** (`/sayim`) | AYNI |
| **M14-4** | `BasitMenu.cs`: `/bildirimler` silindi (rozet hedefi kopar) | `BASIT_MOD_TUTARSIZ_sifir_rozetin_hedefi_basit_modda_da_acilir` · `BasitMenu_listesi_TEK_yerdedir_ve_yedi_yol_icerir` (2) | `BASIT_MOD_TUTARSIZ` = **1** (`rozet:/bildirimler`) | AYNI |
| **M14-5** | `KenarCubugu.razor`: `&& Modu.MenudeGorunur` → `\|\| Modu.MenudeGorunur` (M-UX3-06) | `Basit_modda_menu_YALNIZ_BasitMenu_listesidir` · `Basit_modda_profesyonel_ekranlar_menude_YOKTUR` · `Basit_modda_BOS_grup_basligi_cizilmez` · `Yetkili_TEZGAHTAR_basit_modda_calisma_ekranlarini_gorur` · `Yetkisiz_kullanici_baglantiyi_gormez_PROFESYONEL_modda` · `Yetkisiz_kullanici_baglantiyi_BASIT_modda_da_gormez` · `Yetkisiz_kullanicinin_grubu_HIC_cizilmez` (7) | — | AYNI |

**Test açığı bulunmadı** — beş mutasyonun her biri adıyla kırmızı test verdi.
Son mutasyondan sonra: `dotnet build` → 0 Uyarı 0 Hata.

---

## 8. SAYAÇLAR (`dotnet/denetim.sh`, `# >>> P-14` … `# <<< P-14`)

Ölçüm komutları betiğin içinde **yorum olarak** yazılıdır (BK-07); tabanlar **taze ölçüldü**.

| Sayaç | P-14 öncesi (ölçülen) | Şimdi | Kural |
|---|---|---|---|
| `MENU_AYNI_AD` | **4** (M14-2b ile kanıtlandı) | **0** | mutlak sıfır, tavan artırılmaz |
| `MENU_KAPSAM` (erişilemeyen `@page`) | 0 | **0** (42 rota tarandı) | mutlak sıfır |
| `BASIT_MOD_TUTARSIZ` | 0 | **0** (8 basit yol + rozet hedefleri) | mutlak sıfır |
| `MENU_ELLE_BAGLANTI` (destek) | 38 elle href | **0** elle href · grup başlığı ✓ | mekanizma kilidi |

`MENU_AYNI_AD` ölçüsü (betikte de, `MenuTanimi.Sadelestir` içinde de aynı kural): Türkçe
harfler katlanır → çoğul eki (leri/lari/ler/lar) kırpılır → **ayırt etmeyen genel ek sözcükler**
(defter · rapor · takib/takip · takvim · cihaz · alacak · liste) atılır → hepsi atılırsa ilk
sözcük korunur. İki farklı hedef aynı sadeleşmiş ada inerse çarpışma sayılır.

```
./denetim.sh
  ✓ P-14: MENU_AYNI_AD                             0 (-eq 0: 38 etiket sadeleştirildi)
  ✓ P-14: MENU_KAPSAM                              0 (-eq 0: 42 rota tarandı)
  ✓ P-14: BASIT_MOD_TUTARSIZ                       0 (-eq 0: 8 basit yol + rozet hedefleri)
  ✓ P-14: MENU_ELLE_BAGLANTI                       elle href 0 · grup başlığı çizimi var
DENETİM TEMİZ.
```

> Not: blok eklenirken P-02'nin `GREP_INCLUDE_EKSIK` sayacı bir kez **kırmızı** verdi
> (`$WEB/Pages"/*.razor` include'suz taranıyordu); `--include='*.razor'` ile düzeltildi.
> Gizlenmedi, burada yazılıdır.

---

## 9. CANLI DOĞRULAMA (localhost:**5198** — 5199'a DOKUNULMADI)

API: `ASPNETCORE_ENVIRONMENT=Development dotnet run --project Kuyumcu.Api --no-build
--no-launch-profile --urls http://localhost:5198` (PID saklandı, iş sonunda `kill`).

| # | Adım | Beklenen | Gerçek |
|---|---|---|---|
| 1 | `GET /` | 200 | **200** |
| 2 | 42 `@page` rotasının tamamı (parametreliler `1` ile somutlandı) | hepsi 200 | **42/42 = 200**, 200 olmayan **yok** |
| 3 | `GET /css/app.css` | 200, `.kenar-grup-baslik` var | **200**, eşleşme **2 satır** |
| 4 | `GET /_framework/Kuyumcu.Web.wasm` → UTF-16 dizge taraması | `kenar-grup-baslik`, `musteri-stok`, `para-banka`, `belge-uyum` **var** | hepsi **True** |
| 5 | `GET /_framework/Kuyumcu.Core.wasm` → yeni etiketler | "Günlük İş", "Müşteri ve Stok", "Para ve Banka", "Belge ve Uyum", "Dökümler", "Yönetim", "Hareket Dökümü", "Kasa Hareketleri", "Gecikmiş Alacaklar", "POS Hakedişleri", "Ana menü"/"Main menu" | hepsi **True** |
| 6 | Aynı ikili → **eski** çarpışan adlar | hiçbiri **kalmamalı** | "İşlem Defteri" **False** · "Kasa Defteri" **False** · "POS Alacakları" **False** · "Vade Takibi" **False** · "POS Alacak Defteri" **False** |
| 7 | API durduruldu | `lsof -nP -iTCP:5198 -sTCP:LISTEN` boş | **0 satır** |

**SQL kanıtı** (`docker exec kuyumcu-mysql mysql … kuyumcu_net`, parola değişkenden, ekrana basılmadı):

| Sorgu | Başlangıç | Son |
|---|---|---|
| `SELECT COUNT(*) FROM tenants` | **8** | **8** |
| `SELECT COUNT(*) FROM tenants WHERE ad LIKE 'PAKET%' OR ad LIKE 'P-14%'` | 0 | **0** |
| `SELECT COUNT(*) FROM __EFMigrationsHistory` | **55** | **55** |
| `SELECT COUNT(*) FROM tenants WHERE ad LIKE 'QA-TEST-%'` | 3 | **3** (dokunulmadı) |

**Test kiracısı:** bu paket hiç veri yazmadı → test kiracısı **açılmadı**, dolayısıyla silinecek
kiracı yok. `tenants` sayısı başlangıçtaki değerde (8). `QA-TEST-*` fikstürlerine dokunulmadı.

> **Dürüstlük notu (Anayasa md.1):** kenar çubuğu Blazor WebAssembly'de **istemcide** çizilir;
> curl ile HTML'i alıp grup başlığını göremezsiniz. Oturum açmadan menüyü gerçek tarayıcıda
> göstermek için kimlik bilgisi gerekirdi ve **tahmin edilmedi**. Bunun yerine sunucunun
> **fiilen servis ettiği** istemci ikilisi (wasm) ve CSS indirilip içeriği doğrulandı (adım 3-6);
> çizim davranışı 4 bUnit testiyle ayrıca kanıtlandı. Tarayıcıda göz doğrulaması **QA turuna**
> devredildi.

---

## 10. KİMLİK KAPANIŞ TABLOSU

| Kimlik | Durum | Kanıt |
|---|---|---|
| **Y-9** — kenar çubuğu gruplaması + etiket çarpışmaları | **KAPANDI** | `Kuyumcu.Web/Layout/MenuTanimi.cs:64-155` (6 grup / 38 bağlantı) · `Layout/KenarCubugu.razor:28-45` (gruplu çizim) · `Kuyumcu.Core/I18n/Sozlukler.cs:2898,2902,2904,2918` + `3313,3465,3466,3474,4246` (TR) ve `5310,5314,5316,5330` + `5725,5877,5878,5886,6658` (EN) · testler `MenuMimarisiTestleri` (18) · sayaçlar `MENU_AYNI_AD=0`, `MENU_KAPSAM=0`, `BASIT_MOD_TUTARSIZ=0` · canlı § 9 adım 2-6 |
| **B-63** — erişilebilirlik (isimsiz landmark, başlık düzeni) | **KISMEN** (bu paketin yüzeyinde kapandı) | `KenarCubugu.razor:14` `aria-label="@Dil.T.Nav.MenuEtiketi"` · grup başlıkları `<h2>` (h1→h3 atlaması yok) · `Sozlukler.cs` `Nav.MenuEtiketi` TR+EN. **Kalanı devredildi:** form `aria-invalid`/`role=alert`, `/giris` şifre sıfırlama, sayfa `h1` borçları → ilgili sayfa paketleri (P-15 kabuk, P-18 mobil, P-09 kimlik) |
| **B-65** — metin/adlandırma temizliği, tek kavram iki ad (UX-S31) | **KISMEN** (menü yüzeyinde kapandı) | Menü ile sayfa/katalog adları tek ada hizalandı: `Nav.IslemDefteri`=`RaporSeti.AdIslemDefteri`, `Nav.KasaRaporu`=`AdKasaDefteri`, `Nav.VadeTakibi`=`AdVadeTakibi`=`Vade.Baslik`, `Nav.PosTahsilat`=`Pos.TahsilatBaslik`; test `Ayni_kavram_TEK_ad_tasir_menu_ile_rapor_kataloğu_ayni`. **Kalanı devredildi:** sayfa içi çift başlık / kopyala-yapıştır artıkları → ilgili sayfa paketleri |
| **B-59** — ham teknik değer gösterimi (ULID, ham enum, slug) | **DEVREDİLDİ: P-15/P-16 (ilgili sayfa paketleri)** | Menü yüzeyinde ham değer basılmıyor (38 etiketin tamamı sözlükten; `denetim.sh` `MENU_ELLE_BAGLANTI=0` ile çıplak href de kalmadı). Bulguların kaynağı `/islemler` başarı mesajı gibi **sayfa gövdeleri**; bu paket `Pages/` altına dokunmadı (TEK DOKUNUŞ) |
| **B-68** — boş durum / sayfa başlığı / yönlendirme tutarlılığı | **KISMEN** | Yönlendirme ayağı kapandı: erişilemeyen `@page` = 0 (`MENU_KAPSAM`), menüde ölü bağlantı = 0 (`Menudeki_her_yol_GERCEK_bir_rotadir_ve_baslik_tasir`), boş grup başlığı çizilmiyor (menünün "boş durumu"). **Sayfa gövdesi başlık/boş durum borçları devredildi:** `/vezne` vb. → P-15 ve ilgili sayfa paketleri |

---

## 11. KOŞUL KARŞILAMA TABLOSU

| Koşul | Durum | Kanıt |
|---|---|---|
| Plan bloğu okundu, sözleşme olarak uygulandı | ✅ | § 1 |
| 38 bağlantı gruplanır, grup başlığı > 0 | ✅ | 6 grup; `Cizimde_grup_basligi_VARDIR_…`; `MENU_ELLE_BAGLANTI` |
| 4 etiket çarpışması çözülür, aynı metin iki hedefe gitmez | ✅ | § 2.2; `MENU_AYNI_AD` 4 → 0 |
| Basit mod tutarlılığı (rozeti olan özelliğin sayfası basit menüde) | ✅ | `BASIT_MOD_TUTARSIZ=0`; iki test |
| Sayaçlar denetim.sh'a CIRCIR olarak eklendi, ölçüm komutu yorumda, taban taze | ✅ | § 8; blok `# >>> P-14` / `# <<< P-14` |
| Tavan/eşik artırılmadı | ✅ | üç sayaç da mutlak 0 |
| Migration: HAYIR | ✅ | `__EFMigrationsHistory` 55 → 55 |
| Her iş kuralı için hatayı yeniden üreten test | ✅ | 18 yeni test |
| Mutasyon kanıtı (kırmızı + `cmp`) | ✅ | § 7, 5 mutasyon + 1 taban ölçümü |
| 5198'de canlı doğrulama, 5199'a dokunulmadı | ✅ | § 9 |
| `dotnet/denetim.sh` TEMİZ | ✅ | § 8 |
| Tam süit 0 başarısız | ✅ | 3222 / 0 |
| Test kiracısı silindi (QA-TEST-* korundu) | ✅ | kiracı açılmadı; tenants 8 → 8, QA-TEST-* = 3 |
| DoD sözleşmesi bölümü | ✅ | § 3 |
| TEK DOKUNUŞ — dokunulan sayfalar yazılı | ✅ | § 3 (`Layout/KenarCubugu.razor`; `Pages/` altı 0) |
| Yeni istemci `record` Pages içinde açılmadı | ✅ | `MenuOgesi` `Layout/` altında |
| Yeni CSS'te sabit renk / satır içi style yok | ✅ | § 2.4 |
| Sözlük TR+EN, çıplak metin yok | ✅ | 6 grup başlığı + `MenuEtiketi` TR+EN |
| Mevcut element id'leri değişmedi, yeni id'ler `<önek>-<alan>` | ✅ | yeni id'ler `kenar-grup-<slug>`; mevcut id'ler korundu |
| Razor özniteliğinde `{ }` lambda yok · `disabled="false"` yok | ✅ | `Match="@(...)"` üçlü ifade; disabled kullanılmadı |
| COMMIT YOK, git komutu yok, rm/mv yok, sır ekrana basılmadı | ✅ | yalnız `git status --porcelain` okundu |
| Tahmin 120 ajan-dk | ⚠ | Aşıldı (≈170 ajan-dk). Sebep: plan yalnız 4 çarpışmayı sayıyordu; menü etiketi düzeltilince **sayfa/katalog başlıkları** (RaporSeti, Vade, Pos) menüyle çelişti ve UX-S31 gereği 5 anahtar daha hizalanıp iki mevcut test uyarlandı. Ayrıca `MENU_AYNI_AD`'ın ölçülebilir hâle gelmesi menünün veriye taşınmasını gerektirdi (Sapma 2) |

---

## 12. AÇIK KALANLAR

1. **Kılavuz (P-33) ve yetki matrisi bayatladı** — plandaki bilinen risk. Menü sırası ve dört
   etiket değişti; P-33 kılavuz ekran görüntüleri/menü anlatımı ve yetki matrisi belgesi
   **bu paketten sonra** güncellenecek (plan zaten ikisini sonraya koymuştu).
2. **Gerçek tarayıcıda göz doğrulaması** — oturum gerektirdiği için QA turuna devredildi (§ 9 notu).
3. **B-59 / B-63 / B-65 / B-68'in sayfa gövdesi ayakları** — § 10'da paket adıyla devredildi.
4. **`dotnet/denetim.sh.mutbak` (0 bayt, 18 Eylül)** — bu oturumdan ÖNCE kalmış artık; silme
   kurucu kalemidir (hook `rm` engelliyor), dokunulmadı.
5. **Grup adları ürün dili kararıdır** — "Dökümler / Günlük İş / Para ve Banka" ekip önerisidir;
   kurucu farklı bir adlandırma isterse tek dosyada (`Sozlukler.cs`) değişir.

## 13. KURUCUYA / MALİ MÜŞAVİRE NOTLAR

- **Mali müşavir notu gerekmez** — bu pakette mevzuat hükmü yoktur.
- **Kurucuya:** menüdeki dört ad değişti (İşlem Defteri → **Hareket Dökümü**, Kasa Defteri →
  **Kasa Hareketleri**, Vade Takibi → **Gecikmiş Alacaklar**, POS Alacakları → **POS Hakedişleri**).
  Alışkanlık kırılması yaratabilir; itirazınız olursa tek dosyadan geri alınır, sayaç
  çarpışmayı yine yakalar (yani "eskiye dön" demek "çarpışmayı kabul et" demektir).

---

## 14. DEĞİŞEN DOSYALAR

`git status --porcelain | grep -v -E "/(bin|obj)/"` (Kuyumcu.Tests dizini depoda **hiç
izlenmiyor** — `?? dotnet/Kuyumcu.Tests/`; denetim.sh de izlenmiyor):

| Dosya | Durum |
|---|---|
| `dotnet/Kuyumcu.Web/Layout/MenuTanimi.cs` | **YENİ** |
| `dotnet/Kuyumcu.Web/Layout/KenarCubugu.razor` | değişti |
| `dotnet/Kuyumcu.Web/wwwroot/css/app.css` | değişti (`.kenar-grup-baslik`) |
| `dotnet/Kuyumcu.Core/I18n/Sozlukler.cs` | değişti (7 grup/etiket anahtarı + 9 ad hizalaması, TR+EN) |
| `dotnet/Kuyumcu.Tests/MenuMimarisiTestleri.cs` | **YENİ** (18 test) |
| `dotnet/Kuyumcu.Tests/IkonTestleri.cs` | değişti (menü sırası) |
| `dotnet/Kuyumcu.Tests/RaporEkranTestleri.cs` | değişti (sabit ad → sözlük) |
| `dotnet/denetim.sh` | değişti (`# >>> P-14` bloğu + P-02 include düzeltmesi) |

**COMMIT YAPILMADI.** Çalışma ağacında bırakıldı.
