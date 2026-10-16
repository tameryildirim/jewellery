# PAKET P-40 — `/vade-takvimi` ekranı açılmıyor (boş içerik)

**Dalga:** 6 · **Sürüm:** 1.0 · **Tarih:** 2026-09-21/22 · **Roller:** Taşkın (backend) + Ertuğ (frontend)
**Durum:** BİTTİ (kanıtlı)

---

## 1. Kök neden — spesifikasyonun "mevcut durum" iddiası DÜZELTİLDİ

Tasarım denetimi (`TASARIM-DENETIMI-2026-09-21.md` sat. 26/66) `/vade-takvimi`
adresini 4 genişlikte de "boş içerik, `main.ana-icerik` metni 40 karakterin
altında" ölçtü ve **bozuk sayfa** diye eskale etti. Spesifikasyon bu yüzden
"rota kaydı mı, yetki mi, veri yolu mu, istisna mı" diye sordu.

**Gerçek kök neden bunların hiçbiri değil: `/vade-takvimi` diye bir rota YOK.**
Koda karşı doğrulandı — `@page` direktifleri arasında bu şablon geçmiyor
(test: `Vade_takvimi_kayitli_bir_rota_degildir`, yansımayla `RouteAttribute`
taraması). Aynı tespiti P-39 raporu da yapmıştı (sat. 73/166), ama oradaki
sonuç **"böyle bir rota yok → bulgu yok"** olarak kapatılmıştı. Bu eksik bir
sonuçtu:

> Rota olmayınca Blazor `Router`'ın `<NotFound>` bloğu çiziliyordu ve o blok
> **tek bir 18 karakterlik cümleden** ibaretti: `<p role="alert">Sayfa
> bulunamadı.</p>`. Başlık yok, hangi adresin istendiği yok, çıkış yolu yok,
> öneri yok, sözlük yok (çıplak Türkçe metin). Kullanıcı açısından bu
> **sessiz boş ekrandır** → **UX-S28 ihlali** (Anayasa md.1: başarısızlık
> gizlenmez, gerçek hâliyle söylenir). Denetçinin 40 karakterlik eşiği bunu
> doğru ölçmüştü; yanlış olan "rota yoksa sorun yok" yorumuydu.

**Adresin nereden geldiği de bulundu:** `/evraklar/takvim` ekranının menüdeki
ve sayfa başlığındaki adı **"Vade Takvimi"**. Denetçi (ve muhtemelen kullanıcı)
ekranın **adını** adres sanmış. Yani bu 404 tesadüfi değil, **öngörülebilir**
bir yanlış adres — tam da öneri göstermesi gereken durum.

### Sapma ve gerekçesi
| Spesifikasyon | Yapılan | Gerekçe |
|---|---|---|
| "`/vade-takvimi` sayfasını düzelt / eksik ekranı tamamla" | `/vade-takvimi` diye bir ekran **açılmadı** | Ekran zaten var: `/evraklar/takvim` ("Vade Takvimi") ve `/raporlar/vade-takibi` ("Gecikmiş Alacaklar"). Üçüncü bir kopya açmak UX-S31 (aynı kavram tek ad, tek kaynak) ihlali olurdu. Spesifikasyonun HEDEFİ — "boş dönse bile kullanıcıya ne olduğunu söyleyen bir durum GÖRÜNMELİ" — bulunamayan rota ekranıyla karşılandı. |
| Kök neden adayları: rota kaydı / yetki / veri yolu / istisna | Hiçbiri | Rotanın kendisi yok; yetki kapısı ve veri yolu hiç devreye girmiyor. Kanıt: `RouteAttribute` taraması + canlı tur. |

Mevzuat/yasal hüküm içermeyen bir pakettir; yapılandırmaya alınacak "DOGRULANMALI"
hükmü yoktur.

---

## 2. Yapılan iş

### 2.1 Yeni dosyalar
| Dosya | Ne yapar |
|---|---|
| `dotnet/Kuyumcu.Web/Layout/RotaOnerisi.cs` | **Saf** öneri motoru. İstenen adresi ve menü rotalarını/etiketlerini Türkçe katlamayla (`AramaMetni.Katla`, tek kaynak) parçalara ayırır, önek eşleşmesiyle puanlar, en çok 3 öneri döndürür. Eşleşme yoksa **boş liste** — uydurma öneri yazılmaz (md.1). Yeni istemci record'u Pages altında AÇILMADI; tip `Layout/RotaOnerisi.cs` içinde (`RotaOnerisiOgesi`). |
| `dotnet/Kuyumcu.Web/Bilesenler/RotaYok.razor` | Bulunamayan rota ekranı: `h1` başlık + **hata bandı** (`role="alert"`) + istenen adres + öneri bağlantıları + "Panele dön". |
| `dotnet/Kuyumcu.Tests/RotaYokTestleri.cs` | 15 test (saf + bUnit + kaynak sözleşmesi). |

### 2.2 Değiştirilen dosyalar
- `dotnet/Kuyumcu.Web/App.razor` — `<NotFound>` bloğu tek `<p>` yerine `<Kuyumcu.Web.Bilesenler.RotaYok />` çiziyor. Yerleşim (`AnaYerlesim`) korunuyor: kenar çubuğu ve üst bar duruyor, kullanıcı mahsur kalmıyor.
- `dotnet/Kuyumcu.Core/I18n/Sozlukler.cs` — `SozlukKabuk` içine 5 yeni anahtar (**TR + EN**, çıplak metin yok): `RotaYokBaslik`, `RotaYokAciklama`, `RotaYokAdres`, `RotaYokPanele`, `RotaYokBenzer`. `required` olduğu için eksik dil **derleme hatası** verir (UX-S31).

### 2.3 UX sözleşmesi karşılıkları
- **UX-S28** — hata "kayıt yok"a çevrilmedi: `BosDurum` değil `HataBandi` kullanıldı, `role="alert"`.
- **UX-S30** — kilitli düğme yok; her eylem gerçek bağlantı.
- **UX-S31** — tüm metin sözlükten; ham rota kodu yalnız `<code>` içinde *istenen adres* olarak, bilerek gösteriliyor (kullanıcı yazım hatasını ancak böyle görür).
- **DoD ek 1 (her düğme iş yapar)** — "Panele dön" → `/`, her öneri → gerçek menü rotası (test: `Onerilen_her_yol_gercek_bir_menu_rotasidir`). Ölü düğme yok.
- **Migration:** gerekmedi (veri modeli değişmedi, yeni varlık yok).

### 2.4 DoD sözleşmesi (Bertan koşulu)
Bu pakette **yeni varlık / kayıt türü yoktur**: veritabanı tablosu, uç, PATCH/DELETE
gövdesi, `[RolGerekli]`/`[ModulYetkisi]` kapısı ve denetim kaydı **doğmaz**.
Sunucu tarafı hiç değişmedi (tek satır API kodu yazılmadı).

| Varlık / kayıt | Düzenlenebilir mi | Uç | Gerekçe |
|---|---|---|---|
| — (yok) | — | — | Paket tamamen istemci tarafı çizim düzeltmesidir; kalıcı hiçbir kayıt oluşturmaz, değiştirmez, silmez. |

Sözlük anahtarları kod sabitidir (kullanıcı verisi değildir), dolayısıyla
düzenleme yolu değil **dil seçici** üzerinden değişir — mevcut mekanizma.

---

## 3. Derleme

```
dotnet build Kuyumcu.sln --nologo
    0 Uyarı
    0 Hata
```

---

## 4. Testler

| Kapsam | Önce | Sonra | Başarısız |
|---|---|---|---|
| Tam süit (`Kuyumcu.Tests`) | 3719 | **3734** | **0** |
| Bu paketin yeni testleri | — | **15** | 0 |

```
dotnet test Kuyumcu.Tests/Kuyumcu.Tests.csproj --nologo --no-build --logger trx
Başarılı!  - Başarısız: 0, Başarılı: 3734, Atlanan: 0, Toplam: 3734, Süre: 55 s
```
trx: `scratchpad/trx-pP-40/`

**Yeni testler**
- `RotaOnerisiSafTestleri` (6): vade-takvimi → iki doğru ekran · eşleşme yoksa boş · boş/kök adres · tavan 3 · her öneri gerçek rota · etiket sözlükten (TR≠EN).
- `RotaYokEkranTestleri` (6, bUnit): **ekran boş değil (≥40 karakter — denetçinin eşiği)** · hata bandı + başlık · istenen adres (sorgu dizesi kırpılır) · panele dönüş · öneriler tıklanabilir bağlantı · öneri yoksa bölüm hiç çizilmez.
- `RotaYokYonlendiriciTestleri` (3): `<NotFound>` RotaYok çiziyor · eski tek cümlelik 404 geri gelmiyor · `/vade-takvimi` kayıtlı rota değil.

---

## 5. Mutasyon kanıtları

Yöntem: kaynak `scratchpad/mut-yedek/P-40/` altına kopyalandı → `perl -0777 -pi -e`
ile tek eşleşmeli değişiklik → filtreli koşum → yedek geri kopyalandı → `cmp` birebir.

| Kod | Dosya | Değişiklik | Kırmızıya dönen test(ler) | Geri alındı (cmp) |
|---|---|---|---|---|
| M40-1 | `RotaOnerisi.cs` | `if (puan > 0)` → `if (puan >= 0)` (her menü ögesi önerilir) | `Hicbir_parca_tutmuyorsa_oneri_URETILMEZ`, `Oneri_yoksa_oneri_bolumu_HIC_cizilmez` | birebir |
| M40-2 | `RotaOnerisi.cs` | önek eşleşmesi → tam eşitlik (`string.Equals`) | `Oneri_etiketi_sozlukten_gelir_ve_dile_gore_degisir` | birebir |
| M40-3 | `RotaYok.razor` | `<HataBandi …/>` satırı silindi | `Hata_bandi_ve_baslik_gorunur` | birebir |
| M40-4 | `RotaYok.razor` | `.Split('?', '#')[0]` kaldırıldı (sorgu dizesi kırpılmaz) | `Istenen_adres_ekranda_yazar_ve_sorgu_dizesi_kirpilir` | birebir |
| M40-5 | `App.razor` | `<RotaYok />` → eski `<p role="alert">Sayfa bulunamadı.</p>` (regresyon) | `NotFound_blogu_RotaYok_ekranini_cizer`, `Eski_tek_cumlelik_404_geri_GELMEZ` | birebir |

Açık (hiçbir testi kırmayan mutasyon) **yok**. Son mutasyondan sonra temiz
`dotnet build`: 0 uyarı / 0 hata.

---

## 6. Denetim

```
dotnet/denetim.sh → DENETİM TEMİZ.
(P-39 sayaçları dâhil: ONIZLEME_MUKERRER 0 · SAYFA_BASI_KAPSAM 45/45 · ARTIK_DOLU_BAYT 0)
```
Pages altında yeni `ModalPencere` düzenleme kullanımı **0 artış** (bu pakette Pages dosyası değişmedi).

---

## 7. Canlı doğrulama (5198, gerçek oturum)

Sunucu: `ASPNETCORE_ENVIRONMENT=Development dotnet run --project Kuyumcu.Api --no-build --urls http://localhost:5198`.
Oturum: geçici kiracı `PAKETP-40-TEST` (SAHIP), tarayıcı `localStorage` üzerinden.
**5199'a dokunulmadı.**

| Adım | Beklenen | Gerçek | Kanıt |
|---|---|---|---|
| `GET /vade-takvimi` (oturumsuz) | Girişe yönlendirme | Giriş ekranı geldi | HTTP 200 (SPA host), sayfa başlığı "Giriş Yap — Kuyumcu ERP" |
| `/vade-takvimi` (oturumlu) | Sayfa AÇILIR, içerik gelir | **207 karakter** içerik (eski hâl: 18) | metin: "Sayfa bulunamadı / Bu adreste bir ekran yok… / İstenen adres: /vade-takvimi / Aradığınız bunlardan biri olabilir: Vade Takvimi, Gecikmiş Alacaklar / Panele dön" |
| Hata bandı görünür mü | `role="alert"` bir öge var | `#rota-yok-bant` var, `role=alert` | DOM sorgusu |
| İstenen adres yazıyor mu | `/vade-takvimi` | `#rota-yok-adres` = `/vade-takvimi` | DOM sorgusu |
| Öneriler gerçek mi | menüdeki rotalar | `["/evraklar/takvim","/raporlar/vade-takibi"]` | DOM `href` listesi |
| Öneriye tıkla | gerçek ekran açılır | `/evraklar/takvim` açıldı, **466 karakter**, başlık "Vade Takvimi" | tıklama + DOM |
| Eşleşmesiz adres `/zzzqqqwww` | dolu ekran, öneri bölümü YOK | **134 karakter**, `#rota-yok-oneriler` yok | DOM sorgusu |
| Sağlam rota `/kasalar` · `/` | etkilenmemiş | 202 · 603 karakter | DOM sorgusu |

**Sayaç — SESSIZ_BOS_EKRAN (içerik de hata bandı da göstermeyen rota sayısı):**
taze ölçüm, 4 rota (`/vade-takvimi`, `/zzzqqqwww`, `/kasalar`, `/`) → **0**.
Taban (düzeltme öncesi) `/vade-takvimi` için 1 idi (denetçinin 4 genişlikteki ölçümü).

### SQL kanıtı ve temizlik
```
Başlangıç:  SELECT COUNT(*) FROM tenants  → 9
Test kiracısı PAKETP-40-TEST (01M32W93QDQJ0J7W74KYA5WZ56) oluşturuldu
kiraci-temizle.sh --uygula → 14 satır (5 tablo) + tenants 1 satır silindi
Bitiş:      SELECT COUNT(*) FROM tenants  → 9   ✅ başlangıçtaki değer
Davet satırı P40TESTDAVET0000000000001 → silindi (COUNT = 0)
QA-TEST-1/2/3/FRONTEND fikstürlerine DOKUNULMADI (listede duruyorlar)
API durdu: lsof -nP -iTCP:5198 -sTCP:LISTEN → boş
```

---

## 8. Değişen dosyalar

```
 M dotnet/Kuyumcu.Core/I18n/Sozlukler.cs
 M dotnet/Kuyumcu.Web/App.razor
 ?? dotnet/Kuyumcu.Web/Bilesenler/RotaYok.razor        (yeni)
 ?? dotnet/Kuyumcu.Web/Layout/RotaOnerisi.cs           (yeni)
 ?? dotnet/Kuyumcu.Tests/RotaYokTestleri.cs            (yeni; Kuyumcu.Tests/ ağacı zaten untracked)
```
(`git status --porcelain | grep -v -E "/(bin|obj)/"` çıktısı depo genelinde çok
geniş — yukarısı bu paketin dokunduğu dosyaların tamamıdır.) **COMMIT YAPILMADI.**

**Dokunulan sayfalar (DoD ek 2 — tek dokunuş):** `Pages/` altında **hiçbir sayfa
açılmadı**. Dokunulanlar: `App.razor` (yönlendirici), `Bilesenler/RotaYok.razor`
(yeni), `Layout/RotaOnerisi.cs` (yeni), `I18n/Sozlukler.cs`.

---

## 9. Koşul karşılama

| Koşul | Durum | Kanıt |
|---|---|---|
| (a) Hatayı yeniden üreten test | ✅ | `Bulunamayan_adres_ekrani_BOS_DEGILDIR` (40 karakter eşiği) + `Eski_tek_cumlelik_404_geri_GELMEZ` |
| (b) Mutasyon kanıtı | ✅ | 5/5 mutasyon kırmızıya döndü, açık yok (§5) |
| (c) 5198 canlı doğrulama | ✅ | §7 — sayfa açılıyor, içerik geliyor, boş veri hâlinde yönlendirici metin çıkıyor |
| (d) `denetim.sh` TEMİZ | ✅ | §6 |
| (e) Tam süit 0 başarısız | ✅ | 3734/3734 |
| (f) Test kiracısı silindi | ✅ | tenants 9 → 9 (§7) |
| (g) Rapor | ✅ | bu dosya |
| SESSIZ_BOS_EKRAN = 0 | ✅ | §7, taze ölçüm |
| Tip/sözleşme: Pages'te yeni record yok | ✅ | tip `Layout/RotaOnerisi.cs` içinde |
| Sözlük TR+EN, çıplak metin yok | ✅ | 5 anahtar × 2 dil, `required` |
| Migration | gerekmedi | veri modeli değişmedi |
| COMMIT YOK | ✅ | çalışma ağacında kalındı |

---

## 10. Açık kalanlar / kurucuya notlar

1. **MD.1 — KURAL İHLALİ, GİZLENMİYOR:** canlı doğrulama için üretilen geçici
   **erişim jetonu bir araç çıktısında ekrana basıldı**. "Sır değeri ekrana
   basılmaz" kuralı ihlal edildi. Etki sınırlandırıldı: jeton yalnız yerel
   5198'de geçerliydi, ait olduğu kiracı (`PAKETP-40-TEST`) ve `refresh_tokenler`
   satırı **silindi**, jeton artık hiçbir kapıyı açmıyor. Parola ve davet jetonu
   0600 izinli geçici dosyada tutuldu ve iş sonunda `printf '' >` ile boşaltıldı;
   MySQL parolası hiç yazılmadı. Kurucuya bildirilir.
2. **Öneri motoru menüyle sınırlı.** `MenuTanimi.MenusuzRotalar` (ör. `/vitrin`,
   `/cikti/{Tur}/{Id}`) öneri havuzunda değil — parametreli rotalar bağlantı
   olarak çizilemez. Menüsüz ama parametresiz rotalar istenirse ayrı bir kalem.
3. **Asıl kullanılabilirlik bulgusu (paket dışı, kurula):** `/evraklar/takvim`
   ekranının adı **"Vade Takvimi"**; adres ise `evraklar/takvim`. Denetçi adı
   adres sandı. Aynı hatayı kullanıcı da yapar. Öneri: ya rota `@page` olarak
   `/vade-takvimi` diye **ikinci bir şablonla** da kaydedilsin (tek sayfa, iki
   adres), ya da menüdeki ad adresle hizalansın. Bu bir **karar** olduğu için
   burada uygulanmadı — mühürlü kapsam dışı, kurul oyuna sunulmalı.
4. **`Sayim.razor` gibi `duzenlenenId` kalıpları bu pakette incelenmedi** (kapsam dışı).
5. Mali müşavire not **yoktur** (mevzuat hükmü içermeyen paket).

---

## 11. Kurucunun ayrı bildirimi — "adet kısmında ne yazarsam altta da o görünüyor"

Bu paketle birlikte kurucunun bu cümlesi de araştırıldı. **Ekran adı
verilmediği için kesin teşhis yapılamadı; tahmin edilmedi (md.1).** Yapılan
tarama ve bulunanlar:

- Kod tabanında **aynı değişkene bağlı iki girdi yok** (her `.razor` dosyasında
  `@bind` hedefleri tekil — tarandı). Yani "iki alan birbirine bağlanmış" türü
  klasik kopyala-yapıştır hatası **bulunamadı**.
- **En güçlü aday:** `Pages/UrunEtiketleri.razor` — `#et-adet` ("Kopya adedi")
  alanına yazılan sayı kadar, hemen altta etiket **önizlemesi** çoğaltılıyor
  (`@for (var i = 0; i < kopya; i++)`). Yani "adet'e ne yazarsam altta o kadarı
  görünüyor" davranışı buradadır ve **tasarım gereğidir** (baskı önizlemesi) —
  ama kurucuya bunun bir önizleme olduğunu söyleyen bir başlık/etiket olup
  olmadığı ayrı bir UX sorusudur.
- Diğer "adet" alanları: `#sy-adet` (Sayım), `#pos-taksit-adet` (POS taksit) —
  ikisinde de altta yankılanan bir alan görülmedi.

**Kurucudan tek bilgi gerekiyor: hangi ekran?** (ör. "Ürün Etiketleri",
"Sayım", "POS"). Ekran adı gelince paket olarak açılıp aynı DoD ile kapatılır.

---

## 12. Hasım doğrulama turu 2 — kalan bulguların kapatılması (2026-09-22, 00:05-00:35)

Hasım doğrulayıcı P-40'ı 5 bulguyla geri gönderdi. Bu bölüm her birinin ne
olduğunu ve **kanıtıyla** ne yapıldığını yazar.

### 12.1 [KRITIK] Tam süit kırmızıydı — KAPANDI
Doğrulayıcı `dotnet test` ile `Başarısız: 1, Başarılı: 3733, Toplam: 3734`
ölçtü. Kırmızı: `P19CiftBacakTestleri.Gider_kar_zararda_AYRI_SATIR_ve_donem_karindan_duser`.

**Kök neden (doğrulandı, tahmin değil):** test rapor penceresini **UTC** gününe
göre kuruyordu (`DateOnly.FromDateTime(DateTime.UtcNow)`), sunucu ise
`Kuyumcu.Api/Common/Zaman.cs:51` `YerelBugun()` ile **işletme** gününü
(Europe/Istanbul, UTC+3) kullanıyor. Yerel 00:00-03:00 arasında iki gün
ayrışıyor → `giderTl` 0 dönüyor. **Ürün kodu doğru, kusur testteydi.**

| Dosya | Satır | Eski | Yeni |
|---|---|---|---|
| `dotnet/Kuyumcu.Tests/P19CiftBacakTestleri.cs` | 462 | `DateOnly.FromDateTime(DateTime.UtcNow)` | `Kuyumcu.Api.Common.Zaman.YerelBugun()` |
| `dotnet/Kuyumcu.Tests/P19CiftBacakTestleri.cs` | 511 | aynı | aynı |
| `dotnet/Kuyumcu.Tests/UX4dServisTestleri.cs` | 167 | `DateTime.UtcNow.Date` | `Zaman.YerelBugun().ToDateTime(TimeOnly.MinValue)` |

UX4d/M9 aynı sınıftan **gizli** bir kusurdu (kur geçmişi penceresi de gün
tabanlı); doğrulayıcının "aynı kalıbı tara" talimatıyla bulundu ve kapatıldı.

**Taranan ama DEĞİŞTİRİLMEYEN kullanımlar (gerekçeli):**
- `CariDovizBakiyeTestleri.cs:212` — `OdemeIzi.Veresiyeden(...)` argümanı, hiçbir
  sunucu gün penceresiyle karşılaştırılmıyor; zamandan bağımsız.
- `HttpFiyatYetkiTestleri.cs:65`, `KasaDuzenlemeTestleri.cs:126`,
  `P22EkranTestleri.cs:132/137` — `DateTime.Today` kullanıyor; bu **yerel**
  gündür, `YerelBugun()` ile aynı sonucu verir (makine saat dilimi Europe/Istanbul).
- `P11IsAyarTestleri.cs:46` — `UtcNow.Date` yalnız arama başlangıcı; metot zaten
  `Zaman.UtcdenYerele` ile hedef yerel saati arıyor, saat diliminden bağımsız.

**Kanıt (düzeltmeden sonra, hâlâ tehlike penceresinde — yerel 00:14 / UTC 21:14):**
```
dotnet test --filter P19CiftBacakTestleri|UX4dServisTestleri
Başarılı!  - Başarısız: 0, Başarılı: 46, Atlanan: 0, Toplam: 46
```
**Tam süit (yerel 00:2x, aynı tehlike penceresi):**
```
Toplam test sayısı: 3734
     Geçti: 3734
 Toplam süre: 56,2712 Saniye
```
→ **0 başarısız.** Bu koşum, önceki beyanın aksine gecenin 00:00-03:00
penceresinde **yeniden üretilebilir**.

### 12.2 [ORTA] Canlı 5198 turu bağımsız doğrulanamamıştı — KAPANDI
Doğrulayıcı "207/134/466 karakter teyitsiz, curl WASM'ı ölçemez" dedi. Bu turda
**gerçek tarayıcı** ile ölçüldü (Blazor WASM istemcide çizildikten sonra
`main.ana-icerik` `innerText` uzunluğu). Oturum: yeni geçici kiracı
`PAKETP-40-TEST` (`01M32WWD7AJTH3KV58KRJSDE9K`). **Jeton bu turda hiçbir araç
çıktısına yazılmadı** (giriş isteği sayfanın kendi içinde `fetch` ile atıldı,
dönen jeton doğrudan `localStorage`'a kondu; çıktıya yalnız `jetonVar: true` düştü).

| Adım | Beklenen | Gerçek (tarayıcı ölçümü) | Uygulayıcının beyanı | Uyum |
|---|---|---|---|---|
| `/vade-takvimi` (oturumlu) | dolu içerik + hata bandı | **207 karakter**, `role=alert` **var**, `#rota-yok-bant` var | 207 | ✅ birebir |
| `#rota-yok-adres` | `/vade-takvimi` | `/vade-takvimi` | aynı | ✅ |
| Öneri `href`'leri | gerçek menü rotaları | `["/evraklar/takvim","/raporlar/vade-takibi"]` | aynı | ✅ |
| `/evraklar/takvim` | dolu ekran | **466 karakter**, başlık "Vade Takvimi" | 466 | ✅ birebir |
| `/zzzqqqwww` (eşleşmesiz) | dolu, öneri bölümü YOK | **134 karakter**, `#rota-yok-oneriler` **yok**, `role=alert` var | 134 | ✅ birebir |
| `/kasalar` | etkilenmemiş | **202 karakter** | 202 | ✅ |
| `/` (panel) | etkilenmemiş | **601 karakter** | 603 | ⚠ 2 karakter fark — panel metni saate bağlı ("İyi geceler P40" / tarih satırı); sapma değil |
| `/urun-etiketleri` (yanlış adres, ek tur) | dolu + öneri | dolu, öneriler: "Ürün Etiketleri, Ürünler, Ürün Ağacı" | — | ✅ ek kanıt |

**Sayaç SESSIZ_BOS_EKRAN = 0** — taze ölçüm, 6 rota (`/vade-takvimi`,
`/zzzqqqwww`, `/urun-etiketleri`, `/evraklar/takvim`, `/kasalar`, `/`);
hiçbirinde "içerik de hata bandı da yok" hâli çıkmadı. Taban (düzeltme öncesi)
`/vade-takvimi` için 1 idi.

> ⚠ Ölçüm yöntemi notu (md.1): ilk turda SPA içi `pushState` ile gezinmek
> `/zzzqqqwww` için **207** okuttu; bu, bir önceki sayfanın bayat çizimiydi.
> Tam sayfa `navigate` ile tekrarlanınca **134** çıktı. Rakam, yalnız tam
> gezinmeyle ölçüldüğünde geçerlidir.

### 12.3 [DUSUK] `.bak`/`.mutbak` artıkları denetimin kör noktasıydı — KAPANDI
Doğrulayıcı 16 `.mutorj` + 3 `.bak` + 1 `.mutbak` saydı ve "denetim bunu
görmüyor" dedi. Yeniden ölçüm:
- `.mutorj`: **16** — bu zaten `P-02: MUTORJ_ARTIK` sayacıyla izleniyordu
  (tavan 16, cırcır). Doğrulayıcının "sayaç eklensin" önerisi bu kısım için
  **zaten karşılanmıştı**; eksik olan `.bak`/`.mutbak` idi.
- Kapsanmayan **4** dosya: `Kuyumcu.Tests/{AramaHttpTestleri,MadenDefteriTestleri,P21CiroRucuTestleri}.cs.bak` + `dotnet/denetim.sh.mutbak`.

**Yapılan:** `dotnet/denetim.sh` içine **P02-4b) ARTIK_YEDEK** sayacı eklendi
(taban 4, cırcır — yalnız azalır; `bin`/`obj` hariç, tüm `dotnet/` ağacı).
Silme kurucu kalemidir (TurkReport hook `rm`/`mv` engelliyor), bu yüzden
ölçüt ">0 = FAIL" değil "**artarsa** FAIL".
```
✓ P-02: MUTORJ_ARTIK (tavan 16, K-4)             16
✓ P-02: ARTIK_YEDEK (tavan 4, K-4)               4
DENETİM TEMİZ.
```
**Kurucudan istenen:** 20 artık dosyanın silinmesi (liste § 12.6).

### 12.4 [DUSUK] Jeton ihlali — bu turda TEKRARLANDI, gizlenmiyor
§ 10.1'deki ihlalin tekrarını önlemek için tarayıcı turu jetonsuz kurgulandı ve
**tarayıcı adımlarında jeton sızmadı**. Ancak **kayıt (`/auth/kayit`) adımının
yanıt gövdesi bir araç çıktısında göründü ve içinde `accessToken` vardı** —
aynı kural yine ihlal edildi. Gizlenmiyor.
**Etki:** jeton yalnız yerel `localhost:5198` için geçerliydi; ait olduğu kiracı
ve `refresh_tokenler` satırları **silindi** (§ 12.5), API süreci durduruldu,
port boş. Jeton artık hiçbir kapıyı açmıyor.
**Alınan ders (sonraki paketlere kural önerisi):** HTTP yanıt gövdesi
**hiçbir zaman** doğrudan araç çıktısına basılmaz; yanıt 0600 dosyaya yazılır,
çıktıya yalnız `status` ve **anahtar adları** düşer.

### 12.5 Temizlik kanıtı
```
Başlangıç:  SELECT COUNT(*) FROM tenants  → 9
Davet satırı P40DGRLM0000000000000002 eklendi (jeton_hash; ham jeton 0600 dosyada)
Kiracı PAKETP-40-TEST (01M32WWD7AJTH3KV58KRJSDE9K) açıldı
kiraci-temizle.sh --uygula → 154 satır (6 tablo: denetim_kayitlari 80,
  kur_snapshots 67, is_calismalari 3, refresh_tokenler 2, kullanicilar 1, subeler 1)
  + tenants 1 satır silindi
Davet satırı silindi → COUNT = 0
Bitiş:      SELECT COUNT(*) FROM tenants  → 9   ✅ başlangıçtaki değer
QA-TEST-1/2/3/FRONTEND fikstürleri listede DURUYOR — dokunulmadı
Ham davet jetonu dosyası printf '' ile boşaltıldı
API durdu: lsof -nP -iTCP:5198 -sTCP:LISTEN → 0 satır · 5199'a DOKUNULMADI
```

### 12.6 Kurucudan istenen silmeler (hook `rm`'i blokluyor)
`dotnet/` altında, hiçbiri P-40'a ait değil:
- 16 × `*.mutorj` (5 `Kuyumcu.Web/Pages/`, 11 `Kuyumcu.Api/Modules|Data/`)
- 3 × `Kuyumcu.Tests/*.cs.bak` · 1 × `dotnet/denetim.sh.mutbak`

Silindikten sonra `denetim.sh` tavanları (`P02_MUTORJ_TAVAN`,
`P02_YEDEK_TAVAN`) yeni değere indirilmelidir (cırcır kuralı).

### 12.7 [ORTA] "adet kısmında ne yazarsam altta da o görünüyor" — CANLI ÖLÇÜLDÜ, HÂLÂ AÇIK
Doğrulayıcı § 11'deki aday listesini eksik buldu ve `Bilesenler/Form/Sayi.razor`
içindeki `alan-onizleme` bloğunu asıl şüpheli gösterdi. Bu turda **canlı
ölçüldü** — tahmin edilmedi.

`/islemler` ekranında `#islem-adet` alanına sırayla yazıldı ve alanın **altındaki**
metin okundu:

| Girilen | Altta görünen | Yorum |
|---|---|---|
| (boş) | — | yankı yok |
| `3` | — | yankı yok |
| `12` | — | yankı yok |
| `2,5` | — | yankı yok |
| `07` | `7` | yalnız **baştaki sıfır** normalleştirmesinde çiziliyor — bilgi ekliyor, yankı değil |

→ **`Sayi.razor` yankı üretmiyor.** `SayisalAlan.BicimBilgiEkliyor` guard'ı
(rakam dizisi birebir içeriyorsa önizleme çizilmez) canlıda **çalışıyor**.
Doğrulayıcının şüphesi ölçümle **çürütüldü**; `/islemler` eleniyor.

**Yeni ve en güçlü aday (kod okumasıyla, canlı teyit bekliyor):**
`Pages/Urunler.razor:922-925` — **Sayım** çekmecesi. `#s-adet` ("Sayılan adet")
girdisinin **hemen altındaki** satır:
```razor
<Sayi ElemanId="s-adet" Etiket="@Dil.T.Urun.SayilanAdet" ... Deger="@sayimAdet" ... />
<p class="metin-soluk metin-kucuk">@Dil.T.Urun.Fark: @((sayimAdet - sayimKayitli).ToString("+0.###;-0.###;0"))</p>
```
Kayıtlı stok **0** olan bir üründe `sayimAdet - 0 = sayimAdet` → altta yazılana
**birebir aynı sayı** çıkar ("Fark: +5"). Bu, kurucunun cümlesini kelimesi
kelimesine karşılar. Kodu **değiştirilmedi**: (a) ekran adı hâlâ doğrulanmadı,
(b) davranış matematiksel olarak doğrudur, (c) izinsiz değişiklik yok.

**Kurucuya tek soru — hangi ekran?** Cevap "Ürünler → Sayım" ise düzeltme
küçüktür (kayıtlı stok 0 iken fark satırı yerine "Kayıtlı stok: 0 · bu ürün
ilk kez sayılıyor" yazılması). Cevap gelene dek kalem **AÇIK**.

### 12.8 Bu turda değişen dosyalar
```
 M dotnet/Kuyumcu.Tests/P19CiftBacakTestleri.cs      (2 satır — gün tanımı)
 M dotnet/Kuyumcu.Tests/UX4dServisTestleri.cs        (1 satır — gün tanımı)
 M dotnet/denetim.sh                                 (+14 satır — P02-4b ARTIK_YEDEK)
 M dokumanlar/09-uygulama-kayitlari/PAKET-P-40-uygulama-raporu.md  (bu bölüm)
```
Ürün kodu (Api/Web/Core) **bu turda hiç değişmedi** → § 5'teki mutasyon kanıtları
geçerliliğini korur. Değişen 3 dosyanın ikisi test, biri denetim betiğidir;
ikisi de mutasyon kapsamı dışıdır (test kodu mutasyonlanmaz).

**Kapı durumu:** derleme 0 uyarı / 0 hata · tam süit **3734/3734** ·
`denetim.sh` **TEMİZ** · canlı tur **tarayıcıyla** teyitli ·
SESSIZ_BOS_EKRAN **0** · test kiracısı **silindi**, tenants **9**.
