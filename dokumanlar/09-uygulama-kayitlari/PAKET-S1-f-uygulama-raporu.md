# PAKET S1-f — Ayarlar → Görünüm kartı (yeni)

**Durum: BİTTİ** (paket kapısı 1–7 sağlandı; ayrıntı § 13).
- Build: 0 hata, 0 uyarı.
- Paket testleri: 41/41 yeşil (35 yöntem).
- Mutasyon: 19/19 kırmızı, her biri geri alındı ve `cmp` ile aynı olduğu doğrulandı.
- `denetim.sh`: **DENETİM TEMİZ**.
- Canlı doğrulama 5194'te yapıldı. Test kiracısı silindi, `tenants` 10 → 11 → 10.
- Tam süitte 14 kırmızı var, hepsi `B4AltinBacagi*`. Sahibi B-4 şeridi. Bu paketin ürettiği kırmızı 0.

Rol: Taşkın (backend) + Ertuğ (frontend). Tek doğruluk kaynağı: tuval `tuval/project/Ayarlar.dc.html` (artboard 7) ve `canvas.json` notu n4.

---

## 1. Spesifikasyon koda karşı doğrulandı

| İddia (görev metni) | Koddaki gerçek (ölçüldü) | Sonuç |
|---|---|---|
| "`kullanicilar` tablosunda tema/sema/yazi_boyutu kolonu YOK → migration gerekir" | Kolonlar **var**. B-1 ekledi, migration `20260923135612_B1KullaniciGorunumTercihi`. `information_schema` sorgusu `tema_modu, sema, yazi_boyutu, arayuz_modu` döndürdü. | **Migration gerekmedi.** |
| "`GorunumServisi.cs:31` yalnız 3 şema; seçim SAHİP'e kilitli (`GorunumKilitli => !SahipMi`)" | `Kuyumcu.Web/Servisler/GorunumServisi.cs` **8 şema + 3 punto** taşıyor (B-1). Kullanıcı düzeyi `PATCH /api/v1/kullanici/gorunum` `[HerkeseAcik]`. SAHİP kilidi yalnız **dükkân varsayılanında** kalmış (`Ayarlar.razor:644`). | Hedef (her kullanıcı seçer, sahip varsayılanı belirler) sunucuda zaten vardı. Bu paket ekranı tuvale göre kurdu. |
| "kiracı varsayılanı tenants tarafında" | Görünüm varsayılanı `kiraci_ayarlari`'nda (`gorunum_semasi`, `gorunum_yazi_olcegi`, `gorunum_tema_modu`). Mod varsayılanı `tenants.varsayilan_arayuz_modu`'da. | Bu iki kaynak aynen kullanıldı. |
| Mod her kullanıcıya açık mı? | **Hayır.** `PATCH /kullanicilar/ben` "herkes" diye belgeli. Ama sınıf düzeyindeki `[ModulYetkisi("kullanici")]` TEZGAHTAR ve MUHASEBE'yi **403** ile durduruyor; bu iki rolün varsayılan matrisinde `kullanici` modülü yok. Canlıda ölçüldü (§ 10, L3). | Sapma S9: dar bir uç açıldı. |

Mevzuat hükmü yok. Bu pakette vergi, MASAK ya da has kuralına dokunulmadı.

## 2. Yapılanlar

**Ekran — `Kuyumcu.Web/Bilesenler/GorunumKarti.razor` (yeni)**

Tuvalin dört bölümü birebir kuruldu:
1. **Renk teması**: 8 karo, 4 sütun, 12 px aralık. Her karoda önizleme var (menü şeridi %34, gövde, kart çubuğu, vurgu çubuğu) ve seçili olanda 3 px vurgu kenarı ile "Seçili" rozeti görünür. Sağda "8 tema · herkes kendi seçer · dükkân varsayılanını sahip belirler" yazar.
2. **Aydınlık / karanlık**: Aydınlık · Karanlık · Bilgisayara uy. Düğmeler 52 px, seçilende 2 px vurgu kenarı ve onay ikonu var. Altında "Fiş ve etiket kâğıdı her zaman beyaz basılır." notu durur.
3. **Yazı boyutu**: Normal 16 px · Büyük 18 px · Çok büyük 20 px. Her düğme kendi puntosuyla yazılır. Not: "Rakamlar her boyutta bir kademe büyük görünür."
4. **Ekran modu**: Basit ("Tezgâh için: tek fiş, büyük düğmeler") ve Profesyonel ("Tüm alanlar açık, kısayollar"). Not: "Hiçbir özellik kapanmaz…"
5. **Alt satır**: tuvaldeki not, ardından "Dükkân varsayılanına dön" (ikincil) ve "Bunu dükkân varsayılanı yap" (birincil, yalnız SAHİP).
6. Başarı bandı `#gk-basari` ve hata bandı `#gk-hata`.
7. Kilit sebepleri: `#gk-kilit`, `#gk-dukkana-don-kilit`, `#gk-varsayilan-yap-kilit`.

Kartın davranışı:
- Seçili durum ve kaynak rozeti **sunucunun** `/kullanici/gorunum` yanıtından gelir. Kart katman çözümünü kendisi yapmaz.
- Kart `ApiIstemci` enjekte etmez; istekler servislerden geçer.

**`Pages/Ayarlar.razor`**
- `<GorunumKarti />` sayfa başındaki bandın hemen altına kondu.
- Eski dört kart **silinmedi**: `#ayar-tema`, `#ayar-benim-gorunum`, `#ayar-gorunum`, `#ayar-arayuz-modu`. Hepsi `<details id="ayar-gorunum-ayrinti">` ("Ayrıntılı görünüm ayarları…") altına katlandı. Mevcut id'ler aynen duruyor.

**Servisler (Web)**
- `KullaniciGorunumServisi`: `HepsiniDevretAsync()` üç alanı tek PATCH'te `null` gönderir. `OkumaDenendi` bayrağı "okunuyor" ile "okunamadı" durumlarını ayırır.
- `GorunumServisi`: `VarsayilanYapAsync(sema, olcek, temaModu)` → `PATCH /gorunum` (SAHİP).
- `ArayuzModuServisi`:
  - `KendiModunuYazAsync(mod)` → yeni dar uç. Etkin mod sunucu yanıtından okunur.
  - `DukkanVarsayilaniYapAsync(mod)` → mevcut `PATCH /admin/tenant`.

**Backend — `Modules/Kullanici/KullaniciModulu.cs`**
- Yeni uç: `PATCH /api/v1/kullanicilar/ben/arayuz-modu` (`KendiArayuzModuController`, `[Authorize]` + `[HerkeseAcik(gerekçe)]`, `[ModulYetkisi]` **yok**).
- Kapsamı dar tutuldu:
  - yalnız `arayuz_modu` alanını yazar;
  - hedef satır **jetondan** gelir;
  - `null` gönderilirse dükkân varsayılanı devralınır;
  - gövdede alan yoksa 400 döner; beyaz liste dışı değer de 400 döner;
  - `DenetimServisi.YazmaKaydet` çağrılır.
- Uç `{arayuzModu, etkinArayuzModu}` döner. Etkin değer `ArayuzModu.Coz` ile çözülür.
- `denetim.sh` "ArayuzModu yalnız 6 dosya" kuralı korundu: uç izinli dosyada durur.

**Sözlük**
- `Kuyumcu.Core/I18n/GorunumKartiSozlugu.cs` (yeni, TR + EN, 23 alan). Ortak hub `Sozlukler.cs`'e dokunulmadı.
- `GorunumSozlugu.cs`'te üç ad tuvale uyduruldu (UX-S31):
  - `sistem` → "Bilgisayara uy";
  - 16/18/20 punto → "Normal / Büyük / Çok büyük";
  - `fildisi` → "Fildişi (açık menü)".

**CSS**
- `app.css` sonuna tek blok eklendi: "S1-f — AYARLAR → GÖRÜNÜM KARTI". Yalnız belirteç kullanır; sabit renk, `rgba`, `overflow` ve `@media print` içermez. Tek kırılım var (768 px), yeni kırılım noktası açılmadı.
- `belirtecler.css`: 8 **aydınlık** şema bloğunun seçicisine `.gorunum-sema-onizleme[data-sema="…"]` eklendi. Böylece önizleme karosu şemanın kendi belirteçlerini satır içi stil ve sabit renk olmadan alır. Karanlık bloklara eklenmedi.

## 3. Sapmalar ve gerekçeleri

| # | Tuval / spesifikasyon | Uygulanan | Gerekçe |
|---|---|---|---|
| S1 | "migration gerekir" | Migration yok | Kolonlar B-1'le geldi, ölçüldü (§ 1). |
| S2 | "şema 8'e çıkarılacak, seçim her kullanıcıya açılacak" | Sunucu zaten öyleydi; ekran kuruldu | B-1 yapmıştı (§ 1). |
| S3 | Üst başlıkta sekmeler: Görünüm · Dükkân · Kur · Yazıcı · Güvenlik | Kurulmadı | Paket kapsamı "Görünüm kartı". Sekmeler bütün Ayarlar sayfasını ve başka sayfaları (İşletme, Kur, MFA) yeniden düzenler; S-4 işidir. |
| S4 | Kaynak rozeti tuvalde yok | Her bölüm başlığında küçük rozet ("Dükkân varsayılanı" / "Sizin seçiminiz" / "Sistem varsayılanı") | UX-S21 bağlayıcı: değerin nereden geldiği söylenir. |
| S5 | Kilit sebebi tuvalde yok | Kilitli düğmenin altında sebep satırı + `aria-describedby` | UX-S30 ve DoD EK md.1. |
| S6 | Seçenek düğmesi kenarı `#c9d0db` | `var(--ayrac)` (`#d7dce4`) | Sabit renk yasak ve bu değerin belirteci yok. En yakın belirteç kullanıldı; tema karolarındaki `#d7dce4` zaten `--ayrac`'a birebir eşit. |
| S7 | "✓" karakteri | `<Ikon Ad="@Ikonlar.Onay" />` | `denetim.sh` "Layout+Bilesenler emoji ikon = 0" sayacı U+2713'ü sayar; ilk denemede 2 ihlal çıktı ve düzeltildi. |
| S8 | — | Eski dört kart katlandı | Plan § 3 md.1: özellik silinmez. Alan alan "devral", dükkân varsayılanının tek tek seçicileri ve cihaz teması orada yaşıyor. |
| S9 | Modu "her kullanıcı seçer" | Yeni dar uç `PATCH /kullanicilar/ben/arayuz-modu` | Ayrıntı aşağıda. |
| S10 | "Dükkân varsayılanına dön" / "Bunu dükkân varsayılanı yap" tek hamle | İki istek gidiyor: önce görünüm, sonra mod. İkinci adım düşerse hata bandı kısmi sonucu **açıkça** yazar ("… döndü; ekran modu döndürülemedi: …"). | Anayasa md.1: "hepsi oldu" denmez. `S1f_E7c` ve `S1f_E10b` testleri bunu kilitler. |
| S11 | Testlerde `KUYUMCU_TEST_DB=kuyumcu_test_s` | İlk filtreli koşumlar `_s` ile yapıldı. Mutasyonlar ve tam süit `kuyumcu_test_s1f` ile koşuldu. | Başka bir şerit aynı anda `kuyumcu_test_s`'i siliyordu. Ölçülen hata: `MySqlException: Unknown database 'kuyumcu_test_s'` (11/11 test 1 ms'de düştü; `processlist`'te `_s`'e başka bağlantı vardı). |
| S12 | Önizleme | Karolar şemanın **aydınlık** hâlini gösterir | Tuval böyle. |

**S9'un ayrıntısı:**
- `PATCH /kullanicilar/ben` TEZGAHTAR ve MUHASEBE'ye 403 döndürüyor (canlıda L3 ile ölçüldü).
- Bu ucu herkese açmak yanlış olurdu: aynı gövde kendi `subeId` ve `email` alanını da değiştirir.
- İlk denemede mod `/kullanici/gorunum`'a eklendi. `denetim.sh` "ArayuzModu yalnız 6 dosya" kapısı kırmızı döndü (8 dosya) ve bu değişiklik **geri alındı**. `GorunumModulu.cs` ve `KullaniciGorunumModulu.cs` baştaki satır sayılarına döndü (335 / 230).

## 4. Migration
**Gerekmedi.** Kolonlar mevcut (B-1). `__EFMigrationsHistory` = 70, en son `20260923174110_B3PesinMusteriSistemCarisi`. Bu paket hiçbir şema değişikliği yapmadı.

## 5. DoD sözleşmesi (Bertan koşulu)
Yeni varlık açılmadı. Kart üç mevcut tercih kaydını ve bir mevcut kiracı alanını düzenler.

| Kayıt | Düzenleme ucu | Silme / sıfırlama | Kapı | Denetim | Ekrandaki yol |
|---|---|---|---|---|---|
| Kullanıcı görünümü (`kullanicilar.sema/tema_modu/yazi_boyutu`) | `PATCH /api/v1/kullanici/gorunum`: gövdede yoksa korunur | `null` = dükkân varsayılanına devret | `[HerkeseAcik]`, kimlik jetondan | `KullaniciGorunum` (çağrılır) | Karolar/düğmeler + "Dükkân varsayılanına dön" |
| Kullanıcı arayüz modu (`kullanicilar.arayuz_modu`) | **Yeni** `PATCH /api/v1/kullanicilar/ben/arayuz-modu` | `null` = devral | `[HerkeseAcik]` (gerekçeli), yalnız kendi satırı ve yalnız bu alan | `Kullanici` · "arayuz-modu: X" (çağrılır) | Basit/Profesyonel + "Dükkân varsayılanına dön" |
| Dükkân görünüm varsayılanı (`kiraci_ayarlari.gorunum_*`) | `PATCH /api/v1/gorunum` | `null` = satır silinir, sistem varsayılanına döner | `[RolGerekli("SAHIP")]` | `Gorunum` (çağrılır) | "Bunu dükkân varsayılanı yap" + katlı `#gorunum-sema/-olcek` |
| Dükkân mod varsayılanı (`tenants.varsayilan_arayuz_modu`) | `PATCH /api/v1/admin/tenant` (mevcut) | Değer değiştirilir; kolon NOT NULL | `[RolGerekli("SAHIP")]` sınıf düzeyinde | **Yok**: mevcut açık, § 14 md.3 | Aynı düğme + İşletme sayfası |

Bu kayıtlar finansal değildir. Hard DELETE yok; sıfırlama `null` ile yapılır.

## 6. Dokunulan sayfalar (DoD EK 2 — tek dokunuş)
- `Pages/Ayarlar.razor`: **gerekçeli tekrar dokunuş.** B-1 (S-B) ve P-31 bu dosyaya yazmıştı. S1-f'in hedefi doğrudan bu sayfanın "Görünüm" bölümü (tuval artboard 7). Değişiklik bir satır `<GorunumKarti />` ile eski kartları saran `<details>` açılış/kapanış satırlarından ibaret. Mevcut kod ve id değişmedi.
- Başka sayfaya dokunulmadı. Yeni bileşen: `Bilesenler/GorunumKarti.razor`.

## 7. Testler

### 7.1 Paket testleri — `Kuyumcu.Tests/S1fGorunumKartiTestleri.cs`: 41/41 yeşil (35 yöntem)

**bUnit (24 durum)**
- E1: dört bölüm, 8+3+3+2 düğme ve tuval metinleri.
- E2: ham kod ekrana basılmıyor.
- E3: seçili durum sunucudan geliyor; tek "Seçili" rozeti var; kaynak rozetleri doğru.
- E4: karo yalnız `sema` gönderiyor ve bant yazılıyor.
- E5/E5b: tema modu kendi alanını, yazı boyutu **sayı** olarak gidiyor.
- E6: mod dar uca gidiyor ve oturum tazeleniyor.
- E7/E7b/E7c: "dön" üç null'ı tek istekte + mod null gönderiyor; yalnız mod kendiyse görünüm isteği atılmıyor; ikinci adım düşerse kısmi hata yazılıyor.
- E8: kendi seçimi yokken "dön" kilitli ve sebepli.
- E9: TEZGAHTAR/MUDUR için "yap" kilitli ve sebepli, kişisel seçim açık.
- E10/E10b: SAHİP üç görünüm alanını + modu gönderiyor, bant özetliyor; mod adımı reddedilirse kısmi hata yazılıyor.
- E11: okunamazsa hepsi kilitli ve `aria-describedby=gk-kilit`.
- E12: okuma sürerken "okunamadı" yazılmıyor.
- E13: hata bandı çıkıyor, başarı bandı çıkmıyor.
- E14: her düğme ya istek atıyor ya kilitli ve sebepli.
- E15: `/ayarlar` yeni kartı taşıyor; eski kartlar katlı ve id'leri yerinde.
- E16: EN çevirisi.

**Saf kaynak testleri (6)**
- K1: razor'da `style=`, `<script`, `ApiIstemci`, hex yok.
- K2: CSS bloğu tuval ölçülerini taşıyor, yalnız belirteç kullanıyor (blok sınırında durur).
- K3: 8 aydınlık blok önizleme seçicisini taşıyor, karanlık bloklar taşımıyor.
- K4: **kâğıt daima beyaz**: yazdırmada gövde `#fff`, kâğıt bildirimleri şema belirtecine başvurmuyor, `gk-` ve önizleme sınıfı kâğıt bağlamına girmiyor.
- K5: sözlük TR/EN dolu ve farklı.
- K6: adlar tuvalle aynı.

**Gerçek HTTP (11 durum)**
- H1: üç null üç alanı temizliyor, mod ayrı kalıyor, ikinci adımda etkin mod dükkânınki oluyor.
- H2: SAHİP → `/gorunum` + `/admin/tenant`; devralan kullanıcı sonucu görüyor; denetim kaydı var.
- H3: TEZGAHTAR iki uçta da 403 alıyor ve satır değişmiyor.
- H4: TEZGAHTAR kendi görünümünü ve modunu yazabiliyor.
- H5: **eski yol 403, dar uç 200.**
- H6: `null` → devral.
- H7: `basit`, `UZMAN` ve `{}` → 400.
- H8: yalnız kendi satırı ve yalnız mod yazılıyor (gövdeye `subeId`, `email`, `id`, `rol` konsa bile); denetim kaydı var.
- H9: jetonsuz istek 401.

### 7.2 Etkilenen mevcut testler
Filtre: B1, Gorunum, Tema, UX1C4, Surum, P31, TasarimBelirtec, S1c/d/e, P36/37/39, MobilParite, RotaBaslik, ArayuzModu, BasitMod, Ayarlar, SozlesmeKapsam, Sozluk, Kullanici, Yetki, Ikon, HerkeseAcik, UX3.
- Sonuç: 687 test, 684 yeşil, 3 kırmızı. Üçü de başka şeritlere ait:
  - `S1cUstBarTestleri.S1c_CSS_blogu…`: S3-1'in Panel bloğundaki `overflow-wrap` yüzünden (S1-c testi dosya sonuna kadar tarıyor). Benim bloğumda `overflow` sayısı 0.
  - `S31PanelSadeTestleri.S31_24`: S3-1 şeridi o anda yazıyordu.
  - `OlcumKapisiTestleri.P02…`: razor `record ` satır sayısı eşzamanlı düzenleme sırasında 4 okundu. Benim razor dosyalarımda `record ` yok.
- Üçü de **tam süitte yeşil** (§ 7.3).

### 7.3 Tam süit
Komut: `dotnet test … -v n --logger trx --results-directory scratchpad/trx-pS1-f`, `build-server shutdown` sonrası. Kayıt: `trx-pS1-f/_Mac_2026-09-23_23_45_29.trx`.

| Koşum | Toplam | Geçti | Başarısız |
|---|---|---|---|
| S1-e sonu (devir) | 4361 | 4356 | 5 |
| **S1-f** | **4541** | **4527** | **14** |

- 14 kırmızının hepsi `B4AltinBacagiTestleri.*` (13) ve `B4AltinBacagiHttpTestleri.AltinBacagi_Http_Fis_Iki_Hareket_Ve_Pusula_Doner` (1). Mesaj: `System.InvalidOperationException : Already committed or rolled back.`
- Sahibi **B-4 şeridi** (S-B; altın bacağı / has defteri, yazım sürüyor). Bu paket Satış/Hareket koduna dokunmadı.
- S1f testleri 41/41 geçti (trx'ten sayıldı).
- Devir listesindeki P48 kırmızıları bu koşumda yeşil.
- **Madde (7):** fark = ∅. Bu paket yeni kırmızı üretmedi.

## 8. Mutasyon kanıtları (19/19 kırmızı)
Yöntem: yedek `scratchpad/mut-yedek/S1-f/` altında → `perl -0777 -i` ile tek eşleşme → filtreli test → yedek geri → `cmp` AYNI. Derleme yarışı çıktığında (CS0016/CS2001/MSB4018, başka şeridin derlemesi) koşum tekrarlandı. Tekrarlar kırmızı sayılmadı.

| Kod | Değişiklik | Kırmızıya dönen test(ler) | Geri alındı |
|---|---|---|---|
| M1f-1 | Dar uçta beyaz liste → `&& false` | `S1f_H7(basit)`, `S1f_H7(UZMAN)` | cmp AYNI |
| M1f-2 | `ArayuzModu = dto.ArayuzModu ?? eski` (null devral bozuldu) | `S1f_H1`, `S1f_H6` | cmp AYNI |
| M1f-3 | "gönderildi mi" kontrolü → `if (false)` | `S1f_H7({})` | cmp AYNI |
| M1f-4 | Dar uca `[ModulYetkisi("kullanici")]` eklendi | `S1f_H1`, `H4`, `H5`, `H7`×3, `H8` | cmp AYNI |
| M1f-5 | `denetim.YazmaKaydet` silindi | `S1f_H8` | cmp AYNI |
| M1f-6 | `Uygula(tercih, Profesyonel)` (sunucunun etkin modu yok sayıldı) | `S1f_E6(#gk-mod-basit)` | cmp AYNI |
| M1f-7 | `HepsiniDevretAsync` → yalnız `SemaGovde(null)` | `S1f_E7` | cmp AYNI |
| M1f-8 | `YapKilitli => Kilitli` (SAHİP kilidi kalktı) | `S1f_E9(TEZGAHTAR)`, `S1f_E9(MUDUR)` | cmp AYNI |
| M1f-9 | Dükkân modu sabit `PROFESYONEL` | `S1f_E10` | cmp AYNI |
| M1f-10 | `Kilitli => calisiyor` (okunamadı kilidi kalktı) | `S1f_E11` | cmp AYNI |
| M1f-11 | Okuma sürerken "okunamadı" | `S1f_E12` | cmp AYNI |
| M1f-12 | Kısmi hata sarmalama → `when (false)` | `S1f_E7c` | cmp AYNI |
| M1f-13 | `DonKilitli => Kilitli` | `S1f_E7`, `S1f_E8` | cmp AYNI |
| M1f-14 | Zümrüt aydınlık bloğundan önizleme seçicisi silindi | `S1f_K3` | cmp AYNI |
| M1f-15 | `.gk-sema.secili` kenarı 3 px → 1 px | `S1f_K2` | cmp AYNI |
| M1f-16 | `@media print` içine `.gk { color: var(--kenar-yuzey) }` | `S1f_K4` (kâğıt beyaz) | cmp AYNI |
| M1f-17 | "Bilgisayara uy" → "Sistem" | `S1f_E1`, `S1f_K6` | cmp AYNI |
| M1f-18 | `Ayarlar.razor`'dan `<GorunumKarti />` silindi | `S1f_E15` | cmp AYNI |
| M1f-19 | `Kaydet` catch: `hata =` → `basari =` | `S1f_E13`, `S1f_E7c`, `S1f_E10b` | cmp AYNI |

Test açığı: hiçbir mutasyon yeşil kalmadı. Son mutasyondan sonra temiz `dotnet build Kuyumcu.sln`: **0 hata, 0 uyarı** (tek analizör uyarısı xUnit2029 düzeltildi).

## 9. Denetim
- İlk ölçüm (paket öncesi): 1 ihlal. `Kültürsüz ToString 28 > 27` başka şeridindi (Raporlar/Gider).
- İkinci ölçüm (ilk tasarım sonrası): kendi iki ihlalimi buldu, ikisi de düzeltildi:
  - `ArayuzModu yalnız 6 dosya → 8`: mod Görünüm modüllerine eklenmişti, geri alındı; dar uç izinli dosyaya kondu.
  - `emoji ikon → 2`: ✓ karakteri; `Ikon`'a çevrildi.
- **Son koşum:** `DENETİM TEMİZ.`
  - Kendi sayaçlarım: ArayuzModu **6** ✓ · kapı yüzeyinde **0** ✓ · satır içi `style=` 113/196 ✓ · `@onclick`'siz düğme 67/71 ✓ · ModalPencere 55/55 ✓ · emoji **0** ✓ · Kültürsüz ToString 27/27 ✓ · P-11 **0** ✓.

## 10. Canlı doğrulama (port **5194**, `--no-build`, Development, gerçek MySQL `kuyumcu_net`)
- Test kiracısı: **PAKETS1-f-TEST Kuyumculuk** (`01M3801ZCYJJGEY3BAW6CF01QX`), sistem daveti ile açıldı.
- Sistem anahtarı ve iki parola rastgele üretildi, yalnız scratchpad'de tutuldu ve ekrana yazılmadı.
- TEZGAHTAR kullanıcısı SAHİP tarafından açıldı ve şube atandı. Şubesiz tezgâhtar `/auth/ben`'de 403 "Şube bağlamı çözülemedi" alıyor; bu mevcut kural ve doğru davranış.

| # | Adım | Beklenen | Gerçek |
|---|---|---|---|
| L1 | `GET /css/app.css` | 200, kaynakla aynı, S1-f bloğu | **200**, `cmp` AYNI, blok 1 |
| L2 | `GET /css/belirtecler.css` | 8 önizleme seçicisi | **200**, **8** |
| L3 | TEZGAHTAR `PATCH /kullanicilar/ben {arayuzModu:BASIT}` | (sapmanın ölçümü) | **403** "Bu ekran için yetkiniz yok (kullanici)" |
| L4 | TEZGAHTAR `PATCH /kullanicilar/ben/arayuz-modu {BASIT}` | 200 | **200** `{"arayuzModu":"BASIT","etkinArayuzModu":"BASIT"}`; `/auth/ben` → `arayuzModu=BASIT etkin=BASIT rol=TEZGAHTAR` |
| L5 | TEZGAHTAR `PATCH /kullanici/gorunum {sema:zumrut, yaziBoyutu:20}` | 200, kaynak kullanıcı | **200** `sema=zumrut ozelSema=zumrut yazi=20 semaKaynagi=kullanici` |
| L5b | Aynı uç, üç null (kartın "dön" gövdesi) | Üçü temizlenir | **200** `ozelSema/ozelTemaModu/ozelYaziBoyutu = null`, `sema=platin`, kaynak `sistem` |
| L6 | Dar uç `{arayuzModu:null}` | Devral | **200** `{"arayuzModu":null,"etkinArayuzModu":"PROFESYONEL"}` |
| L7 | Dar uç `{basit}` / `{}` | 400 / 400 | **400** "Geçersiz arayüz modu…" / **400** "arayuzModu alanı gönderilmedi…" |
| L8 | TEZGAHTAR `PATCH /gorunum`, `PATCH /admin/tenant` | 403 / 403 | **403** / **403** "gerekli rol: SAHIP" |
| L9 | SAHİP `PATCH /gorunum {okyanus,20,karanlik}` + `PATCH /admin/tenant {BASIT}` | 200 + devralan görür | **200** / **200** `varsayilanArayuzModu=BASIT`; TEZGAHTAR `GET /kullanici/gorunum` → `okyanus/kiraci · 20/kiraci · karanlik/kiraci`; `/auth/ben` → `arayuzModu=None etkin=BASIT` |

**SQL kanıtı (kiracı silinmeden önce):**
```
kullanicilar: paket-s1f-test SAHIP arayuz_modu/sema/tema_modu/yazi_boyutu = NULL ×4
              paket-s1f-tezgah TEZGAHTAR NULL ×4   (L5b + L6 sonrası: hepsi devralıyor)
kiraci_ayarlari: gorunum_semasi=okyanus · gorunum_tema_modu=karanlik · gorunum_yazi_olcegi=20
tenants: PAKETS1-f-TEST Kuyumculuk · varsayilan_arayuz_modu=BASIT
denetim_kayitlari: Kullanici EKLEME "arayuz-modu: BASIT"
                   KullaniciGorunum EKLEME "…güncellendi: renk şeması, yazı boyutu"
                   KullaniciGorunum EKLEME "…güncellendi: tema modu, renk şeması, yazı boyutu"
                   Kullanici EKLEME "arayuz-modu: devral"
                   Gorunum EKLEME "Görünüm ayarı güncellendi: renk şeması, yazı boyutu, tema modu"
```

**Tarayıcı:** oturumlu `/ayarlar` tarayıcıda açılmadı, çünkü forma parola yazmak yasak. DOM sözleşmesi bUnit ile, CSS kaynağı L1/L2 ile doğrulandı (§ 14 md.5).

**Temizlik**
- API: `dotnet run` (95851) ve dinleyen süreç (95856) `kill` ile durduruldu. `lsof -iTCP:5194 -sTCP:LISTEN` **boş**. 5195–5200'e dokunulmadı.
  - İlk başlatma denemesi `bin/Debug/net8.0/Kuyumcu.Api` bulunamadığı için düştü: başka bir şerit `bin`'i temizlemişti. `dotnet build Kuyumcu.Api` sonrası ikinci deneme başarılı oldu.
- Kiracı `tenant_id` kolonu olan **70 tablodan** silindi: silme öncesi 202 satır, sonrası **0**. Davet `kiraci_davetleri`'nden silindi (kalan 0). `tenants`'tan silindi.
- `tenants`: başlangıç **10** → 11 → son **10**. `QA-TEST-` fikstürlerine (4 kiracı) dokunulmadı.
- Sır dosyaları `printf "" >` ile boşaltıldı (toplam 0 bayt).

## 11. Değişen dosyalar
`git status --porcelain | grep -v -E "/(bin|obj)/"` süzgeci bu pakete ait şu satırları gösterdi (ağaç tek commit'li, birçok dizin `??`):
```
 M dotnet/Kuyumcu.Api/Modules/Kullanici/KullaniciModulu.cs   (dar "kendi modum" ucu + servis metodu)
 M dotnet/Kuyumcu.Web/Pages/Ayarlar.razor                    (<GorunumKarti /> + <details> sarmalı)
 M dotnet/Kuyumcu.Web/wwwroot/css/app.css                    (S1-f bloğu, dosya sonu)
?? dotnet/Kuyumcu.Web/wwwroot/css/belirtecler.css            (8 aydınlık seçiciye önizleme sınıfı + yorum)
?? dotnet/Kuyumcu.Web/Bilesenler/GorunumKarti.razor          (YENİ)
?? dotnet/Kuyumcu.Core/I18n/GorunumKartiSozlugu.cs           (YENİ)
?? dotnet/Kuyumcu.Core/I18n/GorunumSozlugu.cs                (3 ad tuvale uyduruldu)
?? dotnet/Kuyumcu.Web/Servisler/ArayuzModuServisi.cs         (KendiModunuYazAsync, DukkanVarsayilaniYapAsync)
?? dotnet/Kuyumcu.Web/Servisler/GorunumServisi.cs            (VarsayilanYapAsync)
?? dotnet/Kuyumcu.Web/Servisler/KullaniciGorunumServisi.cs   (HepsiniDevretAsync, OkumaDenendi)
?? dotnet/Kuyumcu.Tests/S1fGorunumKartiTestleri.cs           (YENİ)
```
Denenip **geri alınan** (net değişiklik 0, satır sayıları başlangıçla aynı): `Modules/Gorunum/GorunumModulu.cs` (335), `Modules/Gorunum/KullaniciGorunumModulu.cs` (230), `Kuyumcu.Contracts/Sayfalar/GorunumSozlesmeleri.cs`.

Proje ağacında `.mutorj`, `.bak` ya da `.tmp` artığı bırakılmadı. Mutasyon yedekleri scratchpad'de.

## 12. UX sözleşmesi karşılama

| Madde | Durum | Kanıt |
|---|---|---|
| UX-S21 kaynak rozeti | KARŞILANDI | Dört bölümde rozet (E3) |
| UX-S22 | UYGULANMAZ | Tek seçenekli seçici yok |
| UX-S23 etki aynı ekranda | KARŞILANDI | Seçim anında uygulanır; dükkân varsayılanı bantta özetlenir (E10) |
| UX-S25 düzeltme yolu | KARŞILANDI | Her seçim geri alınabilir: "Dükkân varsayılanına dön" + katlı alan alan devral |
| UX-S26 aynı alan kümesi | KARŞILANDI | Şema, punto ve mod listeleri tek diziden (`GorunumServisi.Semalar/Olcekler`) |
| UX-S27 sonuç görünür | KARŞILANDI | Başarı bandı + sunucunun yazdığı özet (E4, E10) |
| UX-S28 hata ≠ boş | KARŞILANDI | Hata bandı ayrı; okunamazsa tahmini seçim çizilmez (E11, E13) |
| UX-S29 | UYGULANMAZ | Liste yok |
| UX-S30 kilit sebebi | KARŞILANDI | E8, E9, E11, E14 |
| UX-S31 tek ad, ham kod yok | KARŞILANDI | E2, K6; "Sistem" ve "punto" adları tek kaynakta tuvale uyduruldu |
| UX-S32/S33 | UYGULANMAZ | Tutar ve dönem yok |
| DoD EK md.1 (her düğme istek atar ya da kilitlidir) | KARŞILANDI | E14 (18 düğme) |
| DoD EK md.2 (tek düzenleme kalıbı) | KARŞILANDI | Yeni ModalPencere yok (55/55) |
| DoD EK md.4 (yıkıcı eylem onayı) | UYGULANMAZ | Eylemler geri alınabilir tercih; yıkıcı değil |
| Kâğıt daima beyaz | KARŞILANDI | K4 + M1f-16; `@media print` bloğu değişmedi |
| Plan § 3 md.1 (özellik silinmez) | KARŞILANDI | Eski kartlar katlı (E15) |
| Plan § 3 md.7 (id sözleşmesi) | KARŞILANDI | Mevcut id değişmedi; yeni id'ler `gk-*` / `ayar-gorunum-*` önekli |
| Plan § 3 md.8 (CDN/inline script/style yok) | KARŞILANDI | K1; fontlar zaten vendored (S1-b) |

## 13. Koşul karşılama (paket kapısı)

| # | Koşul | Durum | Kanıt |
|---|---|---|---|
| 1 | `dotnet build` 0 hata | KARŞILANDI | 0 hata, 0 uyarı |
| 2 | Paket testleri 0 başarısız | KARŞILANDI | 41/41 (filtreli ve tam süitte) |
| 3 | Kendi sayaçlarım ✓ | KARŞILANDI | DENETİM TEMİZ |
| 4 | Mutasyon kanıtı | KARŞILANDI | 19/19 kırmızı, cmp AYNI |
| 5 | Canlı doğrulama | KARŞILANDI | L1–L9 + SQL |
| 6 | Test kiracısı silindi | KARŞILANDI | 70 tabloda 0 satır; tenants 10 |
| 7 | Yeni kırmızı yok | KARŞILANDI | Tam süit 14 kırmızı, hepsi B-4; fark ∅ |

## 14. Açık kalanlar
1. **Tam süitte başka şeridin kırmızıları:** `B4AltinBacagiTestleri.*` ×13 ve `B4AltinBacagiHttpTestleri.AltinBacagi_Http_Fis_Iki_Hareket_Ve_Pusula_Doner`. Sahibi B-4 (S-B şeridi). Mesaj: "Already committed or rolled back."
2. **Üst bardaki mod anahtarı (`#ust-mod-*`, S1-c) ve katlı eski mod kartı (`#arayuz-*`, UX-3) hâlâ `PATCH /kullanicilar/ben` kullanıyor.** Bu yol TEZGAHTAR ve MUHASEBE'de **403** döner (L3 ile ölçüldü, önceden vardı).
   - Tek satırlık düzeltme: `ArayuzModuServisi.DegistirAsync` gövdesini `KendiModunuYazAsync`'e yönlendirmek.
   - Bu değişiklik S1-c'nin `S1cUstBarTestleri` beklediği yolu değiştirir. Karar orkestratöre ya da S1-c sahibine bırakıldı; başka paketin testine dokunulmadı.
3. **Güvenlik notu (Mehmet):** `PATCH /kullanicilar/ben` (`KendinGuncelle`), `kullanici` modülü olan rollere **kendi `subeId`'sini** değiştirme imkânı veriyor. Bu paket o ucu açmadı; varlığını kayda geçiriyor.
4. **`PATCH /admin/tenant` (`VarsayilanArayuzModuGuncelle`) denetim kaydı yazmıyor.** Önceden vardı; bu paket o uca dokunmadı. Denetim çağrısı eklenmeli.
5. **Tuvaldeki üst sekmeler** (Görünüm · Dükkân · Kur · Yazıcı · Güvenlik) kurulmadı (S3). S-4'e önerilir.
6. **Oturumlu tarayıcı ölçümü yapılmadı** (parola yazmak yasak): 4 genişlikte taşma ve 8 şema × aydınlık/karanlık kontrastı. Kurucu tarayıcı bölmesinde bir kez giriş yaparsa ölçüm aynı oturumda yapılabilir.
7. **Test veritabanı çakışması:** `kuyumcu_test_s` başka bir şeritle paylaşılıyor ve eşzamanlı silindi (S11). Bu paket `kuyumcu_test_s1f` kullandı; o veritabanı test artığı olarak MySQL'de duruyor.
8. **S1-c/S1-d/S1-e CSS testleri dosya sonuna kadar tarıyor.** Koşum sırasında S3-1'in `overflow-wrap`'i S1-c testini kırdı; tam süitte yeşildi. Bu paketin testi kendi bloğunun sonunda durur. Aynı düzeltme o testlere de önerilir.

## 15. Kurucuya / mali müşavire notlar
- **Mali müşavire not yok:** mevzuat hükmü yok.
- **Kurucuya:**
  - (a) Tuvaldeki "Sistem" yerine "Bilgisayara uy", "18 punto" yerine "Büyük" yazıldı; ürünün her yerinde tek ad kullanılıyor.
  - (b) "Bunu dükkân varsayılanı yap" SAHİP'in **o anki** dört seçimini (renk, aydınlık/karanlık, yazı, mod) dükkâna yazar. Kendi seçimini yapmamış herkes bunu görür; kendi seçimi olan kullanıcı etkilenmez.
  - (c) Tezgâhtarın üst bardaki basit/profesyonel anahtarı bugün 403 alıyor (§ 14 md.2). Görünüm kartındaki mod düğmeleri tezgâhtarda **çalışıyor**.
