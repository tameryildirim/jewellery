# PAKET P-D2 — ÜRÜN ADI: "Kuyumcu ERP" → **MİHENK**

> **Dalga:** DEMO HAZIRLIK (26 Eylül cumartesi müşteri demosu — zorunlu işler)
> **Rol:** Taşkın (03 Backend) + Ertuğ (03 Frontend) · Opus uygulayıcı
> **Tarih:** 23 Eylül 2026, 18:00–18:25
> **Spesifikasyon:** `dokumanlar/02-mimari/sadelestirme/DEMO-SENARYOSU-2026-09-26.md`
> § 0 **Z3** · § 3 **V17** · § 4 **D-d** · § 7 Bölüm A **A5**
> **Kurucu kararı (23 Eylül, birebir):** *"ürünün adı mihenk olacak"*.
> MİHENK = mihenk taşı — kuyumcunun altının ayarını ölçtüğü taş.
> **Firma adı ByGatech A.Ş. DEĞİŞMEDİ.**

---

## 1. YAPILAN İŞ

Marka metni **tek kaynağa** bağlandı; eski ürün adı kaynak ağacından tamamen kalktı
ve geri sızmasını kapatan bir **test + denetim sayacı** yazıldı.

### 1.1 Tek kaynak (yeni dosya)

`dotnet/Kuyumcu.Core/I18n/Sozlukler.Marka.cs` — `namespace Kuyumcu.Core.I18n`:

| Üye | Değer / davranış | Nerede kullanılıyor |
|---|---|---|
| `Marka.Ad` | `"MİHENK"` | PageTitle, sözlük `Auth.UygulamaAdi` (TR+EN) |
| `Marka.AdAscii` | `"MIHENK"` | otpauth URI yayıncısı (Türkçe'ye özgü harf taşımayan aktarım yüzeyi) |
| `Marka.Ayrac` | `" — "` | sayfa başlığı ayracı |
| `Marka.KilavuzBasligi` | `Ad + Ayrac + "Kullanım Kılavuzu"` | `kilavuz.html` `<title>` |
| `Marka.YukleniyorMetni` | `Ad + " yükleniyor…"` | `index.html` açılış ekranı |
| `Marka.SayfaBasligi(ad)` | `"<sayfa> — MİHENK"`, ad boşsa yalnız `"MİHENK"` | 44 sayfa PageTitle |

**Neden ayrı dosya:** görev kuralı — `Sozlukler.cs` 9223 satırlık ORTAK HUB'dır ve
aynı anda başka şeritler kullanır. Marka metni oraya değil buraya yazıldı; aynı
namespace olduğu için çağrı yeri değişmedi (`Dil` ile `Marka` yan yana durur).

### 1.2 Bağlanan yüzeyler

| Yüzey | Önce | Sonra |
|---|---|---|
| 44 sayfa `<PageTitle>` | `@Dil.T.X.Baslik — Kuyumcu ERP` | `@Marka.SayfaBasligi(Dil.T.X.Baslik)` |
| `App.razor` `<NotFound>` | `<PageTitle>Kuyumcu ERP</PageTitle>` | `<PageTitle>@Marka.Ad</PageTitle>` |
| `wwwroot/index.html` | `<title>Kuyumcu ERP</title>` + "Kuyumcu ERP yükleniyor…" | `<title>MİHENK</title>` + "MİHENK yükleniyor…" |
| `wwwroot/kilavuz.html` (9 geçiş) | "Kuyumcu ERP …" | "MİHENK …" |
| `Sozlukler.cs` `Auth.UygulamaAdi` (TR+EN, 2 satır) | `"Kuyumcu ERP"` | `Marka.Ad` — giriş ekranı kartı ve `© <yıl>` satırı buradan besleniyor |
| `MfaModulu.cs` `MfaAyarlari.Yayinci` | `"Kuyumcu ERP"` | `Marka.AdAscii` |
| `Kuyumcu.Api/appsettings.json` `Mfa:Yayinci` | `"Kuyumcu ERP"` | `"MIHENK"` |
| `app.css` · `belirtecler.css` başlık yorumu | "Kuyumcu ERP — …" | "MİHENK — …" |
| `kurulum.sh` · `kurulum.ps1` · `yedek-al-ve-dogrula.sh` · `yetim-tara.sh` | başlık/echo metni | "MİHENK" |
| **`start.bat` · `stop.bat`** (kök) — 7 geçiş | "Kuyumcu ERP baslatiliyor…" | **"MIHENK baslatiliyor…"** (ASCII — § 6 S6) |
| `KarZarar.razor` | `<PageTitle>@Dil.T.KarZarar.Baslik</PageTitle>` (marka hiç yoktu) | `@Marka.SayfaBasligi(...)` |

### 1.3 Kapılar (mekanizma — düzeltme değil)

1. **Test:** `dotnet/Kuyumcu.Tests/MarkaAdiTestleri.cs` — 7 yeni test.
2. **Denetim sayacı:** `dotnet/denetim.sh` § **P02-1d) ESKI_URUN_ADI = 0**
   (P02-1b "OLU_IC_BAGLANTI" kapısının hemen ardına, aynı P-02 demo kapı bloğuna).

---

## 2. DoD SÖZLEŞMESİ (Bertan koşulu)

Bu paket **hiçbir yeni varlık/kayıt türü, uç, migration ya da veri yazma yolu
üretmez.** Yalnız kullanıcıya görünen METİN sabitini tek kaynağa bağlar.

| Soru | Cevap | Gerekçe |
|---|---|---|
| Yeni varlık var mı? | **Yok** | Veri modeli dokunulmadı. |
| PATCH / DELETE ucu gerekir mi? | **Gerekmez** | Kayıt türü doğmadı. |
| `[RolGerekli]` / `[ModulYetkisi]` gerekir mi? | **Gerekmez** | Yeni uç yok. |
| Denetim kaydı gerekir mi? | **Gerekmez** | Yazma yolu yok. |
| Ekranda düzenleme yolu gerekir mi? | **Gerekmez** | Kullanıcı verisi değil, **ürün adı** — derleme zamanı sabiti. |
| Düzeltme yolu nedir? | `Sozlukler.Marka.cs` → `Marka.Ad` | Tek satır; bütün ekran, sekme başlığı ve kılavuz oradan türer. |
| `Mfa:Yayinci` düzenlenebilir mi? | **Evet** — `appsettings.json` / ortam değişkeni (`Mfa__Yayinci`) | Zaten yapılandırmadan okunuyordu; koda gömülü varsayılan tek kaynağa bağlandı. |

**Değişmezlik gerekçesi (DegismezIpucu yerine):** ürün adı bir işletme verisi
değil, üründür. Kiracı başına değişmez; bu yüzden ekrana düzenleme düğmesi
konmaz, kaynak sabiti değişir.

---

## 3. SINIR — DOKUNULMAYANLAR ve NEDENİ

| Dokunulmadı | Neden |
|---|---|
| **Yasal belge şablonları** — gider pusulası, fatura, MASAK formları | Görev kuralı: yasal metin ve **firma unvanı** değişmez. Ölçüldü: bu şablonlarda ürün adı zaten **hiç geçmiyordu** (`grep "Kuyumcu ERP"` → 0), yani değiştirilecek bir şey de yoktu. |
| `Pages/PusulaBelgesi.razor` `<PageTitle>` | Gider pusulası **belge çıktısı** sayfası; tarayıcıdan basılır ve birçok tarayıcı sekme başlığını basılan kâğıdın üst bilgisine yazar. Yazılımın adının yasal belgenin üstüne düşmesi istenmez. Başlığında eski ürün adı da hiç geçmiyordu. **Testte gerekçesiyle beyaz listede** (`Muaf` kümesi) ve liste ölü kalmasın diye testin kendisi o sayfanın varlığını doğruluyor. |
| **Firma adı ByGatech A.Ş.** | Kurucu kararı: firma adı değişmez. |
| `dotnet/yayin/linux-x64/**` (yayın anlık görüntüsü, 11 geçiş) | Derleme/yayın çıktısıdır, kaynak değildir. **Z1 gereği yeniden üretilecek**; kaynak temiz olduğu için sonraki yayın da temiz doğar. Hem testte hem denetim sayacında **gerekçesiyle** kapsam dışı. |
| `dokumanlar/**` ve kök HTML raporları (`devir-raporu.html`, `musteri-raporu.html`, `ham/qa-turu-*.json`, betikler) | **Tarihsel kayıttır.** 16 Eylül'de "Kuyumcu ERP" yazan bir QA raporunu bugün MİHENK yapmak kaydı tahrif etmek olur (Anayasa md.1). Kapının kapsamı `dotnet/` kaynak ağacıdır. |
| `SONRAKI-OTURUM-PROMPT.md`, `MIMARI-KALAN-PAKETLER-2026-09-15.md`, paket spesifikasyonu | Görev kuralı: dokunulmaz (orkestratör yazar). |
| `Sozlukler.cs`'in yapısı | Yalnız **2 satırda değer** değişti (`UygulamaAdi = "Kuyumcu ERP",` → `UygulamaAdi = Marka.Ad,`). **SAPMA — § 6'da gerekçelendirildi.** |

---

## 4. DERLEME

```
dotnet build Kuyumcu.sln --nologo     (son koşum, 18:29)
    2 Uyarı
    0 Hata
```
**0 hata.** Kalan 2 uyarının ikisi de
`Kuyumcu.Tests/OlcumKapisiTestleri.cs(525) warning CS8604` — **başka bir şeridin
o anda yazdığı dosya** (bkz. aşağıdaki dürüst kayıt md.2). Bu paketin yazdığı
dosyalarda (`Sozlukler.Marka.cs`, `MarkaAdiTestleri.cs`, 41 `<PageTitle>` satırı,
`denetim.sh` P02-1d bloğu) **0 uyarı**; ara koşumda (18:17) tüm çözüm
`0 Uyarı · 0 Hata` ölçüldü.

⚠ **DÜRÜST KAYIT (Anayasa md.1):** bu paketin ortasında **iki kez** yabancı
ölçüm görüldü ve ikisi de **benim değişikliğimden değil, ağaçta AYNI ANDA
çalışan başka bir şeritten** geliyordu:

1. `18:1x` — `dotnet build` **2 Hata** verdi:
   `Kuyumcu.Web/Pages/Cikti.razor(1,6): error RZ1011` + `RZ2005`.
   Dosyanın 1. satırı `@page@page "/cikti/{Tur}/{Id}"` olmuş ve içine
   `@*MUTASYON M-B1-4: kağıt temaya bağlandı*@` yorumu girmişti — **B-1 şeridinin
   o anda uygulamakta olduğu mutasyon**. 39 saniye sonra (18:16:38) o şerit
   mutasyonu geri aldı, dosya `@page "/cikti/{Tur}/{Id}"` hâline döndü ve
   derleme 0 hata verdi. Dosyaya **dokunmadım**.
2. `Kuyumcu.Tests/OlcumKapisiTestleri.cs(525)` 2 × `warning CS8604` — koşumlar
   arasında **gelip gitti** (aynı şerit o dosyada çalışıyor). Son ölçümde 0 uyarı.

---

## 5. MİGRATION

**Gerekmedi.** Şema değişikliği yok, yeni varlık yok. `dotnet ef` çalıştırılmadı.
Migration sayısı **66** — değişmedi.

---

## 6. SAPMALAR ve GEREKÇELERİ

| # | Sapma | Gerekçe |
|---|---|---|
| S1 | Görev "Sozlukler.cs'i DÜZENLEME" diyordu; **2 satırında değer değiştirildi** | Aynı görevin **KAPI koşulu** "kaynak kodda `Kuyumcu ERP` geçmeyecek" diyor. `Sozlukler.cs:3662` ve `:6550`'de `Auth.UygulamaAdi = "Kuyumcu ERP"` duruyordu ve bu değer **giriş ekranının ortasındaki karta ve `©` satırına** basılıyor — demoda müşterinin göreceği ilk ekran. İki koşul çakıştı; **ölçülebilir olan** (ekranda eski ad) seçildi. Risk küçük tutuldu: yapı, satır sayısı, sıra, anahtar kümesi değişmedi; yalnız **iki dizge sabiti `Marka.Ad` referansına** çevrildi. Ortak hub'ta çakışma yüzeyi 2 satır. |
| S2 | Spesifikasyonun "Birleşik koşullar" bölümü **yok** | `DEMO-SENARYOSU-2026-09-26.md` içinde böyle bir başlık bulunamadı (`grep` → 0). Hedefe sadık kalındı: § 0 **Z3**, § 3 **V17**, § 4 **D-d**, § 7 **A5** koşulları karşılandı. |
| S3 | Spesifikasyon "**43 dosyada**" diyor; ölçüm **44 dosya** | Depo genelinde ölçüldü (23 Eylül 18:05): `grep -rl "Kuyumcu ERP"` → 44 dosya, `Mihenk` → 0 dosya. Rakam farkı spesifikasyonun hedefini değiştirmiyor. |
| S4 | `Mfa:Yayinci` **ASCII** yazımla (`MIHENK`) bağlandı | otpauth URI'de `İ` yüzde-kodlanır (`M%C4%B0HENK`) ve bazı doğrulayıcı uygulamalarda bozuk görünür. Ekranda görünen ad `MİHENK` olarak kaldı. |
| S5 | `KarZarar.razor` başlığı da değiştirildi (eski ad orada hiç yoktu) | Tek kalıp kuralı: "her sayfa başlığı tek kaynaktan" testi onu kaçak olarak buldu. Marka **eklendi**, silinmedi. |

| S6 | `start.bat`/`stop.bat`'ta ad **ASCII** (`MIHENK`) yazıldı | `.bat` Windows konsolunda cp857/850 kod sayfasıyla çalışır; `İ` orada bozuk çizilir. Ekranda (tarayıcıda) ad `MİHENK`. |
| S7 | Kapsam görevde "kaynak kod" idi; **kök `start.bat`/`stop.bat`** de kapsama alındı | Demo ADIM 1: *"Programı açar"*. Kılavuz artık "Masaüstündeki **MİHENK** kısayoluna çift tıklayın" diyor; açılan konsol "Kuyumcu ERP baslatiliyor…" deseydi müşteri demonun ilk 30 saniyesinde iki farklı ad görürdü. Denetim sayacı ve test bu iki dosyayı da kapsar. |

**Mevzuat hükmü:** bu pakette mevzuat hükmü **yoktur** — mevzuat kuralı yazılmadı,
eşik/oran/hadde dokunulmadı. Mali müşavir teyidi gerektiren bir şey üretilmedi.

---

## 7. TESTLER

### 7.1 Yeni testler — `Kuyumcu.Tests/MarkaAdiTestleri.cs` (8 adet)

| Test | Ne ölçüyor |
|---|---|
| `Kaynak_agacinda_eski_urun_adi_kalmadi` | `dotnet/` ağacında (bin/obj/yayin hariç) `.cs .razor .html .css .json .sh .ps1 .js` içinde eski ad = **0**. Tarama <100 dosya dönerse "sayaç oyunlanmış" diye kırılır. |
| `Acilis_ekrani_sekme_basligini_ve_yukleniyor_metnini_yeni_adla_yazar` | `index.html` `<title>` = `Marka.Ad`, açılış metni = `Marka.YukleniyorMetni` (**A5**). |
| `Kilavuz_basligi_tek_kaynakla_ayni` | `kilavuz.html` `<title>` = `Marka.KilavuzBasligi`. |
| `Sozluk_uygulama_adi_iki_dilde_de_tek_kaynaktan_gelir` | TR **ve** EN sözlükte `Auth.UygulamaAdi == Marka.Ad`. |
| `Her_sayfa_basligi_marka_tek_kaynagindan_uretilir` | `Pages/**` içindeki her `<PageTitle>` `Marka.` kullanır (tek muaf: `PusulaBelgesi.razor`, gerekçesi kodda). Muafiyet listesi ölü kalamaz. |
| `Sayfa_basligi_kalibi_ad_ve_ayraci_dogru_kurar` | `SayfaBasligi("Kasa") == "Kasa — MİHENK"`; `null`/boşlukta boş ayraç basılmaz. |
| `Acilis_betikleri_yeni_adi_tasir` | `start.bat` ve `stop.bat` eski adı taşımaz, `Marka.AdAscii` taşır (**demo ADIM 1**: müşteri programı kısayoldan açar, konsolda adı görür). |
| `Denetim_betigi_eski_urun_adi_sayacini_tasiyor` | denetim.sh'taki **P02-1d bloğunun** kendisi: atama + `-eq 0` kararı + `p02_ok`/`p02_kir` dalları + oyunlama dalı + `yayin` elemesi + tek kaynak dosya adı. |

### 7.2 Güçlendirilen mevcut test

`TotpTestleri.Otpauth_uri_anahtari_ve_parametreleri_tasir` — yayıncı artık
`Marka.AdAscii`'den geliyor; `issuer=MIHENK` ve `DoesNotContain("Kuyumcu")`
iddiaları eklendi. **Test sayısı düşmedi, 2 iddia arttı.**

### 7.3 Koşumlar

| Kapsam | Toplam | Başarılı | Başarısız | Atlanan |
|---|---|---|---|---|
| Filtreli — `MarkaAdiTestleri` + `TotpTestleri` | 16 | 16 | 0 | 0 |
| Filtreli — `MarkaAdiTestleri` (güçlendirme sonrası) | 7 | 7 | 0 | 0 |
| Ara TAM SÜİT (18:18 · `trx`) | 4042 | 4041 | 1 | 0 |
| Filtreli — `MarkaAdiTestleri` (açılış betiği testi eklenince) | 8 | 8 | 0 | 0 |
| **SON TAM SÜİT** (18:30 · `trx` — tek kaynak) | **4043** | **4042** | **1** | 0 |

**Önce → sonra:** devir kaydı (23.09 planlı duruş) **4035 test / 16 kırmızı**
ölçmüştü. Şimdi **4043 test / 1 kırmızı**. Artış **+8** = bu paketin yeni testleri.
Kırmızı 16 → 1 düşüşü **benim işim değil**: 15'i B-1 şeridinin (Ayarlar.razor)
işiydi ve o şerit aynı ağaçta çalışırken kapandı.

### 7.4 Kalan tek kırmızı — **BENİM DEĞİL**

```
Kuyumcu.Tests.OlcumKapisiTestleri.denetim_sh_HER_CAGRI_DIZININDEN_AYNI_KARARI_VERIR
  Assert.Contains() Failure: Sub-string not found
  Not found: "ÖLÇÜLEMEDİ(betik kendi yolundan okunamadı"
```

Devir notu bu kırmızıyı **P-30b**'ye (DALGA 7c) yazmıştır: test yazıldı, denetim.sh'taki
fail-closed dalı yazılmadan şerit durduruldu. Devir notu kuralı gereği **dokunulmadı**
(testi silerek yeşile boyamak yasak). Benim eklediğim P02-1d bloğunun bu testle ilgisi
olmadığı ayrıca **ölçüldü**: denetim.sh hem kökten (`bash dotnet/denetim.sh`) hem
`dotnet/` içinden (`./denetim.sh`) koşuldu — **ikisi de rc=0** ve ikisi de
`ESKI_URUN_ADI … 0 (842 dosya tarandı)` yazdı, yani yeni sayaç çağrı dizininden
bağımsızdır.

⚠ Konsol özeti `Başarısız: 3` yazdı, `trx` sayacı `failed=1` diyor ve tek bir
kırmızı test adı listeliyor. Tek kaynak olarak **trx** alındı; fark msbuild'in
özet satırından geliyor, üç ayrı kırmızı test **yok** (trx'te 4041 Passed + 1 Failed).

---

## 8. MUTASYON KANITLARI

Yöntem: kaynak `scratchpad/mut-yedek/P-D2/` altına kopyalandı → `perl -0777 -pi -e`
ile tek eşleşmeli değişiklik → `dotnet build` + filtreli `dotnet test` → yedek geri
kopyalandı → `cmp` ile birebir aynı olduğu kanıtlandı.

| Kod | Değişiklik | Kırmızıya dönen test(ler) | Geri alma |
|---|---|---|---|
| **M1-1** | `App.razor`: `<PageTitle>@Marka.Ad</PageTitle>` → `<PageTitle>Kuyumcu ERP</PageTitle>` | `Kaynak_agacinda_eski_urun_adi_kalmadi` | `cmp` AYNI |
| **M1-2** | `Marka.Ad` = `"MIHENK-BOZUK"` | `Acilis_ekrani_…_yeni_adla_yazar` · `Kilavuz_basligi_tek_kaynakla_ayni` | `cmp` AYNI |
| **M1-3** | `Panel.razor`: `@Marka.SayfaBasligi(Dil.T.Nav.Panel)` → `@Dil.T.Nav.Panel` | `Her_sayfa_basligi_marka_tek_kaynagindan_uretilir` | `cmp` AYNI |
| **M1-4** | `denetim.sh`: `P02_MARKA_N=` ataması `P02_MARKA_NN=` yapıldı (sayaç sessizce ölür) | `Denetim_betigi_eski_urun_adi_sayacini_tasiyor` | `cmp` AYNI |
| **M1-5** | `SayfaBasligi`: boş-ad dalı silindi (`sayfaAdi + Ayrac + Ad`) | `Sayfa_basligi_kalibi_ad_ve_ayraci_dogru_kurar` | `cmp` AYNI |
| **M1-6** | `Sozlukler.cs` **EN** sözlük: `UygulamaAdi = Marka.Ad` → `"Jewelry ERP"` | `Sozluk_uygulama_adi_iki_dilde_de_tek_kaynaktan_gelir` | `cmp` AYNI |
| **M1-7** | `Marka.AdAscii` = `"KuyumcuERP"` | `TotpTestleri.Otpauth_uri_anahtari_ve_parametreleri_tasir` | `cmp` AYNI |
| **M1-8** | `start.bat`: `echo   MIHENK baslatiliyor` → `echo   Kuyumcu ERP baslatiliyor` | `Acilis_betikleri_yeni_adi_tasir` **ve** denetim sayacı: `✗ ESKI_URUN_ADI 1 ≠ 0 — ../start.bat` | `cmp` AYNI |

### 8.1 Kapatılan TEST AÇIĞI (dürüst kayıt)

**M1-4 ilk koşumda HİÇBİR testi kırmadı** (7/7 yeşil). Sebep: testin ilk hâli yalnız
`denetim.sh` içinde `"ESKI_URUN_ADI"` ve `"P02_MARKA_N"` dizgelerinin **geçip
geçmediğine** bakıyordu; atama adını değiştirmek dizgeleri yerinde bıraktığı için
kapı sessizce ölüyor, test yeşil kalıyordu. Test **güçlendirildi**: artık P02-1d
bloğu ayrıştırılıyor ve blokta atamanın (`^P02_MARKA_N=`), kararın
(`${P02_MARKA_N:-99} -eq 0`), iki dalın (`p02_ok`/`p02_kir`), oyunlama dalının
(`P02_MARKA_TARANAN`), `yayin` elemesinin ve tek kaynak dosya adının bulunması
şart. Mutasyon **tekrarlandı → KIRMIZI**.

### 8.2 M1-7'de ölçüm tekrarı (dürüst kayıt)

M1-7 **toplu** koşumda (Totp + Marka filtreleri birlikte) yanlışlıkla **yeşil**
rapor etti. Tek teste indirgenip **yeniden ölçüldü**
(`--filter FullyQualifiedName=Kuyumcu.Tests.TotpTestleri.Otpauth_uri_anahtari_ve_parametreleri_tasir`)
→ **KIRMIZI** (`Başarısız: 1, Başarılı: 0`). Tabloya **yalıtılmış ölçüm** yazıldı.
Toplu koşumdaki yeşil, ağaçta aynı anda yazan başka şeritle birlikte bayat derleme
çıktısı kullanılmasına bağlanıyor — **doğrulanamadı**, o yüzden buraya not düşüldü.

**Son mutasyondan sonra TEMİZ derleme:** `0 Uyarı · 0 Hata` (§ 4).

---

## 9. DENETİM

```
$ bash dotnet/denetim.sh          → rc=0
$ cd dotnet && ./denetim.sh       → rc=0
DENETİM TEMİZ.

  ✓ P-02: ESKI_URUN_ADI (marka tek kaynak · P-D2)   0 (843 dosya tarandı)
```

⚠ **Devir notundaki ihlal kapandı — benim işim değil.** Devir kaydı
`P-11 DIS_HATADA_VERI_DUSUREN_YOL_TOPLAM 48 > tavan 47` ihlalini bildiriyordu.
Paketin ortasında koşulan denetimde (18:0x) ihlal **hâlâ 48** ile duruyordu; paket
sonundaki koşumda **kalmadı**. Devir notu bu ihlali P-42/P-46'ya yazmıştı; o şerit
aynı ağaçta çalışırken kapattı. **Ben P-11'e dokunmadım.**

### Yeni sayaç — P02-1d
Kapsam: `dotnet/` ağacı, `.cs .razor .html .css .json .sh .ps1 .js`.
Elenenler ve gerekçeleri betikte yazılı: `bin/obj` (derleme çıktısı),
`yayin/` (kaynaktan yeniden üretilir), `denetim.sh` (aranan dizgeyi kendisi tanımlar).
Ek olarak kaynak ağacın **dışındaki** iki açılış betiği (`../start.bat`,
`../stop.bat`) adıyla kapsama alınır — kullanıcı onları çift tıklar ve konsolda
ürün adını görür.
Aranan dizge betikte **parça parça** kuruluyor (`"Kuyumcu"" ""ERP"`) ki betik kendini
saymasın. **Beyaz liste boş** — yasal şablon istisnası gerekmedi (§ 3).
Oyunlama kapısı: taranan dosya sayısı <100 ise sayaç KIRILIR.

---

## 10. CANLI DOĞRULAMA (port **5195** — 5196–5200 arasına dokunulmadı)

API: `ASPNETCORE_ENVIRONMENT=Development dotnet run --project Kuyumcu.Api --no-build
--no-launch-profile --urls http://localhost:5195` (PID 88486).

| # | Adım | Beklenen | **Gerçek** |
|---|---|---|---|
| C1 | `GET http://localhost:5195/` | HTTP 200 | **HTTP 200** |
| C2 | Gövdede sekme başlığı | `<title>MİHENK</title>` | **satır 8: `<title>MİHENK</title>`** |
| C3 | Gövdede açılış metni | `MİHENK yükleniyor…` | **satır 22: `<p>MİHENK yükleniyor…</p>`** |
| C4 | Gövdede eski ad | 0 | **0** (`grep -c "Kuyumcu ERP"`) |
| C5 | `GET /kilavuz.html` | HTTP 200 + `MİHENK — Kullanım Kılavuzu` | **HTTP 200 · satır 6: `<title>MİHENK — Kullanım Kılavuzu</title>` · eski ad 0** |
| C6 | `GET /_framework/Kuyumcu.Core.wasm` (istemciye giden gerçek derleme) | yeni ad var, eski ad yok | **HTTP 200 · 1.126.677 bayt · UTF-16LE `MİHENK` = 5 · `Kuyumcu ERP` = 0** |

C6, adın yalnız statik HTML'de değil **tarayıcıya inen derlenmiş istemci
paketinde** de yeni olduğunun kanıtıdır (`strings` ASCII aradığı için 0 döner;
.NET dizgeleri UTF-16LE'dir — ikisi de ölçüldü ve rapora yazıldı).

API durduruldu: `lsof -nP -iTCP:5195 -sTCP:LISTEN` → **0 dinleyici**.

### SQL / kiracı kanıtı

```sql
SELECT COUNT(*) FROM tenants;   -- başlangıç: 9   ·   bitiş: 10
```

⚠ **Bu paket HİÇBİR kiracı oluşturmadı** — canlı doğrulama yalnız statik varlık
`GET` isteğidir, tek bir yazma işlemi yapılmadı. Aradaki +1 kiracının adı
**`PAKETB-1-TEST`** (ölçüldü): aynı ağaçta **o anda çalışan B-1 şeridinin** test
kiracısıdır. Başkasının test kiracısına **dokunulmadı/silinmedi**; `QA-TEST-*`
kalıcı fikstürlerine de dokunulmadı. **P-D2'nin kiracı deltası = 0.**

---

## 11. KOŞUL KARŞILAMA TABLOSU

| Koşul | Durum | Kanıt |
|---|---|---|
| Z3 / V17 / D-d — ekranda ad MİHENK | ✅ | § 10 C1–C6 |
| § 7 A5 — sekme başlığı + açılış ekranı MİHENK, eski ad yok | ✅ | C2, C3, C4 |
| Marka metni TEK KAYNAKTA | ✅ | `Sozlukler.Marka.cs`; § 1.2 tablosu |
| `Sozlukler.cs` ortak hub'ı DÜZENLEME | ⚠ **SAPMA** | 2 satırda **değer** değişti; § 6 S1 |
| Yeni dosya `Kuyumcu.Core/I18n/Sozlukler.Marka.cs`, aynı namespace | ✅ | dosya mevcut, `namespace Kuyumcu.Core.I18n` |
| Yasal belge şablonları / firma unvanı değişmedi | ✅ | § 3 |
| Dokunulmayanlar rapora yazıldı | ✅ | § 3 |
| Denetim sayacı `ESKI_URUN_ADI = 0` | ✅ | § 9 |
| Mutasyon: eski ad geri yazılırsa test KIRMIZI | ✅ | M1-1 |
| Test dosyalarını önce tara, kırılanı sabite bağla, **test sayısı düşmesin** | ✅ | `grep -c` → yalnız `TotpTestleri.cs`; sabite bağlandı, 4035 → **4042** (+7) |
| `KUYUMCU_TEST_DB=kuyumcu_test_d` her koşumda | ✅ | tüm `dotnet test` çağrıları |
| Canlı port 5195; 5196–5200'e dokunulmadı | ✅ | § 10 |
| Her koşumdan önce `dotnet build-server shutdown` | ✅ | ilk ve son koşum öncesi |
| Aynı anda koşan şeritlerin dosyalarına dokunulmadı | ✅ | Kasa/Vezne/Maliyet/Rapor/Raporlar/Gider · yayinla.sh/qa-sunucu.sh/kapi.sh/Kurulum.razor · SiparisDetay/Siparisler/Faturalar — **PageTitle dışında hiçbirine dokunulmadı**; § 13 |
| Kendi paketime ait olmayan kırmızıya dokunulmadı | ✅ | § 7.4 |
| COMMIT YOK | ✅ | `git add/commit/push` hiç çalıştırılmadı |
| Migration | — | gerekmedi (§ 5) |
| Mevzuat kuralı | — | bu pakette yok (§ 6) |

---

## 12. DEĞİŞEN DOSYALAR

**Yeni (2):**
- `dotnet/Kuyumcu.Core/I18n/Sozlukler.Marka.cs`
- `dotnet/Kuyumcu.Tests/MarkaAdiTestleri.cs`

**Değişen (56):**
- `dotnet/denetim.sh` (P02-1d sayacı)
- `dotnet/Kuyumcu.Web/App.razor`
- `dotnet/Kuyumcu.Web/wwwroot/index.html` · `wwwroot/kilavuz.html`
- `dotnet/Kuyumcu.Web/wwwroot/css/app.css` · `wwwroot/css/belirtecler.css`
- `dotnet/Kuyumcu.Core/I18n/Sozlukler.cs` (2 satır — § 6 S1)
- `dotnet/Kuyumcu.Api/Modules/Auth/MfaModulu.cs` · `dotnet/Kuyumcu.Api/appsettings.json`
- `dotnet/Kuyumcu.Tests/TotpTestleri.cs`
- `dotnet/kurulum.sh` · `kurulum.ps1` · `yedek-al-ve-dogrula.sh` · `yetim-tara.sh`
- `start.bat` · `stop.bat` (depo kökü)
- `dotnet/Kuyumcu.Web/Pages/` — **41 sayfa** (yalnız `<PageTitle>` satırı):
  Ayarlar · Banka · BankaEkstre · Bildirimler · Cariler · Cikti · DenetimIzi ·
  Emanet · EvrakTakvimi · Evraklar · Faturalar · Fiyatlandirma · Gider ·
  GiderPusulalari · Giris · Hurda · IslemDefteri · Islemler · Isletme · KarZarar ·
  KasaRaporu · Kasalar · Kilavuz · Kullanicilar · Kur · Panel · Pos ·
  PosTahsilatlar · Raporlar · SatisAnalizi · Sayim · SiparisDetay · Siparisler ·
  UrunAgaci · UrunEtiketleri · Urunler · Uyum · VadeTakibi · Vezne · Vitrin · Yetkiler

**Dokunulan sayfalar (MÜHÜR 2 · TEK DOKUNUŞ kaydı):** yukarıdaki 41 sayfanın
**yalnız `<PageTitle>` satırı**. Gövde, id'ler, düğmeler, akış **değişmedi** —
bu yüzden bu sayfalar başka bir dalgada gerekçesiz tekrar açılabilir sayılmalıdır
(P-D2 dokunuşu içerik dokunuşu değildir; kapı kaydına bu not düşülsün).

---

## 13. AÇIK KALANLAR

1. **Yayın anlık görüntüsü (`dotnet/yayin/linux-x64/**`) hâlâ eski adı taşıyor**
   (11 geçiş). **Z1** zaten yayını yeniden üretmeyi emrediyor; üretildiğinde
   düzelir. Bu paket derleme çıktısına elle dokunmaz.
   → **Kapı:** yayın yeniden üretildikten sonra `grep -c "Kuyumcu ERP" dotnet/yayin`
   **0** olmalı; değilse yayın betiği kaynaktan üretmiyordur.
2. **Kök HTML raporları ve `dokumanlar/**` tarihsel kayıt olarak eski adı taşır**
   (§ 3). Kurucu isterse **müşteriye gösterilen** iki dosya (`musteri-raporu.html`,
   `devir-raporu.html`) ayrı bir kalemle güncellenebilir — **tarihsel QA kayıtları
   güncellenmemeli**.
3. **Mevcut MFA kayıtları**: doğrulayıcı uygulamalarına daha önce eklenmiş hesaplar
   eski yayıncı etiketini göstermeye devam eder (etiket, gizli anahtarın parçası
   değildir — kod üretimi **etkilenmez**). Yeni kayıtlar `MIHENK` görür.
4. **Ölü kırmızı** `OlcumKapisiTestleri.denetim_sh_HER_CAGRI_DIZININDEN_AYNI_KARARI_VERIR`
   → **P-30b** (§ 7.4). Bu paket dokunmadı.
5. **Ağaçta artık dosya:** `dotnet/denetim.sh.mutbak` — başka bir şeridin
   mutasyon yedeği (benim değil; benim yedeklerim scratchpad'de). Silme kurucuya
   bırakıldı (TurkReport hook `rm` bloklar).
6. **`deploy/` (10 dosya) ve `docs/` (5 dosya) hâlâ eski adı taşıyor — BİLİNÇLİ
   BIRAKILDI.** Ölçüldü:
   · `deploy/systemd/*.service|*.timer` → `Description=Kuyumcu ERP …`
   · `deploy/kuyumcu-service-kur.ps1` → Windows servisi `DisplayName= "Kuyumcu ERP"`
   · `deploy/nginx-kuyumcu.conf` · `deploy/iis-web.config` → başlık yorumu
   · `docs/DEPLOY.md` · `DOTNET.md` · `MIMARI.md` · `KULLANIM_KILAVUZU.md` ·
     `RUNBOOK-PILOT-DAVET.md` → belge başlıkları
   **Neden dokunulmadı:** (a) bunlar **üretim yerleştirme tanımlarıdır**, demoda
   ekranda görünmez; (b) `deploy/nginx-kuyumcu.conf`, `deploy/iis-web.config` ve
   `deploy/ecosystem.config.js` bu oturumda **başka bir şerit tarafından zaten
   değiştirilmiş** durumdaydı (`git status` M) — YAYIN şeridinin alanı; (c) servis
   `DisplayName`'ini değiştirmek T-3 yedek betiklerinin servis arama yolunu
   etkileyebilir ve **doğrulanmadı**. → **Ayrı kalem olarak orkestratöre düşülsün**
   (YAYIN şeridiyle birlikte, servis adı arama yolu ölçülerek).
7. **Ölçülmedi:** tarayıcıda gerçek görsel tur yapılmadı (bu paketin kapsamı
   metin tek kaynağıdır). Sekme başlığının tarayıcıda göründüğü **A5 maddesi**
   § 7 Bölüm C prova turunda gözle teyit edilmeli.

---

## 14. KURUCUYA NOTLAR

- **Marka/alan adı hâlâ kurucuda.** Bu paket yalnız **yazılımın ekrandaki adını**
  değiştirdi. Ticari unvan, logo, alan adı ve marka tescili kapsam dışı.
- **Adı değiştirmek artık tek satır:** `dotnet/Kuyumcu.Core/I18n/Sozlukler.Marka.cs`
  → `Marka.Ad`. (Statik `index.html`/`kilavuz.html` metinleri derlenmediği için
  oraya elle yazılır; testler ikisinin **ayrışmasını** kırmızıya çevirir.)
- **Mali müşavire not gerekmiyor:** bu pakette mevzuat hükmü, eşik, oran ya da
  yasal belge metni değişmedi.
- **Slogan İngilizce sözlükte hâlâ "Jewelry Management System"** — ürün adı
  çevrilmiyor ama slogan çeviriliyor. Bu bilinçli bırakıldı; değişmesi
  isteniyorsa ayrı kalem.

---

*Rapor: Taşkın + Ertuğ · P-D2 · 23 Eylül 2026 18:32. Ölçümler bu ağaçta,
aynı anda başka şeritler yazarken alınmıştır; yabancı ölçümler § 4, § 7.3,
§ 9 ve § 10'da adıyla ayrıştırılmıştır.*
