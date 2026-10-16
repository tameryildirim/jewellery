# PAKET S-3c (2. dilim) — Engel 20'nin çekirdeği: aralık/risk kapısı ÜÇ yazma yoluna daha

> Dalga 3-H · Uygulayıcı: Taşkın (backend) + Ertuğ (frontend) · Tarih: 2026-09-18
> Kapsam: **S-3c doğrulama artıklarının kapatılması**. Yeni ekran/modül/özellik AÇILMADI
> (kurucu talimatı 13 — kapsam kilidi).
> Not: birinci dilimin raporu `PAKET-S-3c-uygulama-raporu.md`'dir; o rapor bu dosyayla
> birlikte okunur ve **durumu 'BITTI' değil 'KISMEN'dir** (§ 9).

## 1. Özet — ne kapandı, ne kapanmadı

Hasım doğrulayıcının [KRİTİK] bulgusu doğruydu: kapı yalnız
`ListeOlustur` (FiyatModulu.cs:197) ve `ListeGuncelle` (:222) çağrılarına konmuştu.
Aynı iki alan (`hasMarjYuzde`, `iscilikCarpanYuzde`) **üç yazma yolundan daha**
uyarısız yazılıyordu:

| Yol | Uç | Eski hâl | Yeni hâl |
|---|---|---|---|
| `UrunFiyatiEkle` | POST `/fiyat/listeler/{id}/urun-fiyatlari` | marj −5 → **200** | onaysız **400**, onaylı 200 |
| `UrunFiyatiGuncelle` | PATCH `.../urun-fiyatlari/{urunId}` | çarpan 480 → **200** | onaysız **400**, eski değer korunur |
| `TopluUygula` | POST `.../toplu-uygula` | çarpan 480 → **200**, yüzlerce ürün | onaysız **400**, **hiçbir satır yazılmaz** |

Ayrıca:
* **DTO aralıkları sert sınırla hizalandı**: `UrunFiyatiDto` ve `TopluUygulaDto`
  `[Range(-100,1000)]/[Range(0,1000)]` → `[Range(-100,500)]/[Range(0,500)]`
  (`FiyatAraliklari.MarjSert*/CarpanSert*` ile aynı).
* **Ekran tarafı**: `FiyatListesiUrunPaneli.razor`'a `Fiyatlandirma.razor`'daki
  `fl-risk` kalıbının aynısı iki yere eklendi — ürüne özel form (`flp-ozel-risk*`)
  ve toplu uygulama (`flp-toplu-risk*`) — uyarı + onay kutusu + **kilitli Kaydet/Onayla**
  + kilidin sebebi (UX-S30). Değer değişince onay DÜŞER.
* **Canlı doğrulamada bulunan ek kusur düzeltildi**: mesaj yalnız İLK riskli alanı
  söylüyordu ("çarpanı 480 yaptım, bana marjdan bahsediyor"). Artık her riskli alan
  adıyla yazılır; testle (M20-15) ve mutasyonla (M-H) kilitlendi.

Kapsam kilidi gereği **devredilenler** (§ 9): 375 px tablo kartlaştırma → **S-3d**,
T-4a hata kimliği/sürüm damgası → **T-4a**.

## 2. Yeni/değişen sözleşme

`Kuyumcu.Core/Sozlesmeler/FiyatAraliklari.cs` — kısmi gövde desteği:

```csharp
public const decimal MarjNotr = 0m, CarpanNotr = 100m;
public static string? SertSinirDisiKismi(decimal? marj, decimal? carpan);
public static string? RiskKoduKismi(decimal? marj, decimal? carpan);
```

Gerekçe: ürüne özel/toplu gövdede alanlar **nullable**'dır ve "gönderilmedi" =
"listeden devral" demektir, riskli değer demek değildir. Verilmeyen alan nötr
değerle değerlendirilir; eşik hâlâ **tek kaynaktadır** (UX-S31).

`FiyatModulu.AraligiDogrula` ortak imzaya çıkarıldı:

```csharp
private static void AraligiDogrula(decimal? marj, decimal? carpan, bool? riskOnayi,
    decimal? oncekiMarj = null, decimal? oncekiCarpan = null)
```
Liste yolu (`AraligiDogrula(FiyatListesiDto, FiyatListesi?)`) bunun ince bir
sarmalayıcısıdır — birinci dilimin davranışı **değişmedi** (eski 7 test yeşil kaldı).

**Önizleme vs. yazma:** `TopluUygula` kapısı metodun İLK satırındadır.
`?onizleme=true` çağrısında **sert sınır yine reddedilir**, ama risk ONAYI aranmaz —
kullanıcı etkiyi görmeden onay veremez (UX-S23). Yazma çağrısında onay zorunludur.
Canlı kanıt § 7 satır 7.

## 3. Derleme

```
dotnet build Kuyumcu.sln --nologo  →  0 Hata · 0 Uyarı   (üç koşumda da)
```

## 4. Migration

**Gerekmedi** — şema değişikliği yok (yalnız doğrulama kapısı, DTO alanı ve ekran).
`dotnet ef` çalıştırılmadı.

## 5. Testler

| Koşum | Toplam | Başarılı | Başarısız | Not |
|---|---|---|---|---|
| Önce (taban) | 2507 | 2507 | 0 | bu dilimden önce |
| `--filter HttpFiyatAralikTestleri` | 15 | 15 | 0 | 7 eski + 8 yeni |
| `--filter FiyatListesiUrunGorunumuTestleri` | 10 | 10 | 0 | 7 eski + 3 yeni |
| **Tam süit (son)** | **2518** | **2518** | **0** | trx: `scratchpad/trx-pS-3c` |

**Yeni test sayısı: 11** (8 HTTP + 3 bUnit).

Yeni testler:
`URUN_OZEL_eksi_marj_onaysiz_400_doner_ve_kayit_YAZILMAZ`,
`URUN_OZEL_eksi_marj_riskOnayi_ile_KABUL_edilir`,
`URUN_OZEL_PATCH_riskli_degere_gecis_onaysiz_400_doner`,
`URUN_OZEL_degismeyen_riskli_deger_yeniden_onay_ISTEMEZ`,
`URUN_OZEL_sert_sinir_disi_deger_400_doner`,
`TOPLU_UYGULA_riskli_deger_onaysiz_400_doner_ve_HICBIR_fiyat_yazilmaz`,
`TOPLU_UYGULA_sert_sinir_disi_ONAYLA_BILE_400_doner`,
`RISK_mesaji_HER_riskli_alani_adiyla_soyler`,
`TOPLU_riskli_deger_uyarir_ve_onay_kutusu_isaretlenmeden_YAZMA_YOK`,
`TOPLU_risk_onayi_deger_degisince_DUSER`,
`URUN_OZEL_riskli_deger_KAYDET_kilitler_onayla_gonderilir`.

## 6. Mutasyon kanıtları

Yöntem: kaynak `scratchpad/mut-yedek/S-3c/` altına kopyalandı → `perl -0777 -pi -e`
ile tek eşleşmeli değişiklik → filtreli `dotnet test` → yedek geri kopyalandı →
`cmp` ile birebir aynılık kanıtlandı (hepsi "cmp AYNI").

| Kod | Değişiklik | Kırmızıya dönen test(ler) | Geri alma |
|---|---|---|---|
| M-A | `UrunFiyatiEkle` kapı çağrısı silindi | `URUN_OZEL_eksi_marj_onaysiz_400_doner_ve_kayit_YAZILMAZ` | cmp AYNI |
| M-B | `UrunFiyatiGuncelle` kapı çağrısı silindi | `URUN_OZEL_PATCH_riskli_degere_gecis_onaysiz_400_doner` | cmp AYNI |
| M-C | `TopluUygula` kapı çağrısı silindi | `TOPLU_UYGULA_riskli_deger_onaysiz_400_doner_ve_HICBIR_fiyat_yazilmaz` | cmp AYNI |
| M-D | `TopluUygulaDto` `[Range(0,500)]` → `(0,1000)` **+** kapı çağrısı silindi | `TOPLU_UYGULA_riskli_deger_...` ve `TOPLU_UYGULA_sert_sinir_disi_ONAYLA_BILE_400_doner` | cmp AYNI |
| M-E | `flp-toplu-onayla` disabled'ından `TopluRiskBekliyor` çıkarıldı | `TOPLU_riskli_deger_uyarir_ve_onay_kutusu_isaretlenmeden_YAZMA_YOK` | cmp AYNI |
| M-F | `flp-ozel-kaydet` disabled'ından `OzelRiskBekliyor` çıkarıldı | `URUN_OZEL_riskli_deger_KAYDET_kilitler_onayla_gonderilir` | cmp AYNI |
| M-G | `@bind:after="RiskSifirla"` kaldırıldı (toplu marj) | `TOPLU_risk_onayi_deger_degisince_DUSER` | cmp AYNI |
| M-H | Risk mesajında ikinci alan susturuldu | `RISK_mesaji_HER_riskli_alani_adiyla_soyler` + 3 test daha | cmp AYNI |

**Dürüstlük notu (Madde 1):** M-A ve M-C'de sert-sınır testleri (`..._sert_sinir_disi_...`)
YEŞİL kaldı — çünkü o durumu DTO `[Range]` yakalıyor. Yani DTO aralığı tek başına
mutasyona duyarlı değildir; yük taşıdığı **M-D birleşik mutasyonuyla** kanıtlandı
(Range gevşetilip servis kapısı da kaldırılınca test kırmızıya döndü).

## 7. Canlı doğrulama (5198)

Kiracı: `PAKETS3C-TEST Kuyumcu` (`01M2RSZB9PNYYBRTDR3D1AY5E5`) — davet jetonuyla
açıldı (F-2-min hattı), doğrulama sonunda **silindi** (§ 8). 5199'a dokunulmadı.

| # | İstek | Beklenen | Gerçek |
|---|---|---|---|
| 1 | POST `urun-fiyatlari` marj −5, onaysız | 400 | **400** "Has marjı eksi: … Devam etmek için riskOnayi alanını işaretleyin" |
| 2 | Aynı gövde `riskOnayi:true` | 200 | **200**, `hasMarjYuzde:-5` |
| 3 | PATCH `urun-fiyatlari/{id}` çarpan 480, onaysız | 400 + iki alan adı | **400** "Has marjı eksi …; İşçilik çarpanı %200'ün üzerinde …" |
| 4 | Aynı PATCH `riskOnayi:true` | 200 | **200**, `iscilikCarpanYuzde:480` |
| 5 | PATCH yalnız `{"not":"toptanci"}` (riskli değer DEĞİŞMİYOR) | 200 | **200** — kayıt kilitlenmiyor |
| 6 | POST `toplu-uygula` çarpan 480, onaysız | 400, yazma yok | **400** "İşçilik çarpanı %200'ün üzerinde…" |
| 7 | POST `toplu-uygula` çarpan 900, `riskOnayi:true` | 400 (sert sınır) | **400** `{"iscilikCarpanYuzde":["… 0 ile 500 arasında olmalıdır"]}` |
| 8 | POST `toplu-uygula?onizleme=true` çarpan 480, onaysız | risk kapısı GEÇİLİR | **404** "Kur kaydı bulunamadı" (kapı değil, kur yokluğu) |

SQL kanıtı (kapatmadan önce):
```
select count(*), max(has_marj_yuzde), max(iscilik_carpan_yuzde)
from fiyat_listesi_urun_fiyatlari where tenant_id='01M2RSZB9PNYYBRTDR3D1AY5E5';
→ 1   -5.000   480.00        (yalnız ONAYLI yazmalar; onaysız istekler satır bırakmadı)
```

## 8. Temizlik

* API (5198) yalnız kendi PID'i ile durduruldu: `lsof -nP -iTCP:5198 -sTCP:LISTEN` → **0 satır**.
* Test kiracısı `tenant_id` kolonu olan **49 tablodan** silindi + davet satırı silindi.
  `select count(*) from tenants` → **8** (başlangıçtaki değer). `QA-TEST-*` fikstürlerine
  **dokunulmadı**.
* Ağaca `.mutorj/.bak/.tmp` **bırakılmadı** (bu dilimde üretilen mutasyon artığı: 0).

## 9. Önceki doğrulama bulgularının kapanışı

| Bulgu | Durum | Kanıt |
|---|---|---|
| [KRİTİK] Engel 20 çekirdeği açık (3 yazma yolu kapısız, ekranda uyarı yok) | **KAPANDI** | § 1, § 5, § 6, § 7 |
| [ORTA] Paket 'BITTI' ilan edilmişti; iki kalem devredildi | **DÜZELTİLDİ** | Bu paketin durumu **KISMEN**; engel 20 kurul kaydında **AÇIK** kalır: 375 px kartlaştırma → **S-3d**, sürüm damgası → **T-4a** |
| [ORTA] "29 rota taranır" kanıtsız, "5 rota" yanlış | **ÖLÇÜLDÜ** | `grep -rl '@page' Pages/` → **42 rota**. `SatirIciDuzenle` kullanan: **4 sayfa** (Cariler, Islemler, Sayim, VeriIceAktarim). tbody içinde ham girdi taşıyan 5 sayfa (Faturalar, Kurulum, Siparisler, UrunEtiketleri, Yetkiler) **satır içi düzenleme değildir**: bunlar tek Kaydet ile kaydedilen form sepetleridir (satır başına kaydetme yok) → Enter/Esc/blur sözleşmesi uygulanmaz. **İkinci `duzenlenenId` kalıbı YOK**: `Fiyatlandirma.razor`'daki tek eşleşme 13. satırdaki **yorum metnidir**, durum alanı değildir (`grep -n "duzenlenen" Pages/Fiyatlandirma.razor` → yalnız satır 13). Önceki doğrulamanın "ayrı duzenlenenId kalıbı duruyor" iddiası bu ölçümle **düzeltilmiştir**. |
| [DÜŞÜK] Artık dosya sayısı eksik bildirilmişti | **DÜZELTİLDİ** | `find dotnet -name '*.mutorj'` → **16**; `MadenDefteriTestleri.cs.bak` → **1** (0 bayt); `Migrations` altında **8 adet 0 baytlık .cs**. Hiçbiri S-3c'ye ait değil; silme hook nedeniyle **kurucu temizlik listesinde** kalır. |
| [DÜŞÜK] 5199 dinlemiyor | **DOKUNULMADI (gözlem)** | `lsof -nP -iTCP:5199 -sTCP:LISTEN` → 0 satır. Bağlayıcı kural "5199'a dokunma" olduğu için yayın anlık görüntüsü **yeniden başlatılmadı** → QA turu 4 öncesi bunu **orkestratör/kurucu** yapmalıdır, aksi hâlde eski sürüm ölçülür. |

## 10. DoD sözleşmesi

| Varlık | Düzenleme yolu | Pasife alma / geri dönüş | Yetki | Denetim izi | Ekran |
|---|---|---|---|---|---|
| `FiyatListesi` (mevcut) | PATCH `/fiyat/listeler/{id}` | `aktif:false` ↔ `true` | `[RolGerekli("SAHIP","MUDUR")]` | mevcut | `/fiyatlandirma` — `fl-risk` uyarı + onay |
| `FiyatListesiUrunFiyati` (mevcut) | PATCH `.../urun-fiyatlari/{urunId}` (gönderilmeyen alan korunur, null siler) | `aktif:false` → "Geçersiz kılmayı kaldır" (**hard DELETE yok**) | `[RolGerekli("SAHIP","MUDUR")]` | `DenetimeYaz` (çağrıldı, yeniden yazılmadı) | `flp-ozel-form` — risk uyarısı + onay + kilitli Kaydet |
| Toplu uygulama (kayıt değil, işlem) | Düzeltme yolu: aynı uçla yeniden uygula ya da tek tek PATCH | Salt okunur değil; önizleme + parmak izi onayı | `[RolGerekli("SAHIP","MUDUR")]` | `YazmaKaydet("FiyatListesiTopluUygulama", …)` | `flp-toplu-*` — risk uyarısı + onay + kilitli Onayla |

**Yeni varlık açılmadı**, yeni istemci record açılmadı (denetim.sh Pages record tavanı
korunuyor — DENETİM TEMİZ).

## 11. Denetim çıktısı

```
dotnet/denetim.sh  →  DENETİM TEMİZ.   (çıkış kodu 0, iki koşumda da)
```

## 12. Değişen dosyalar (bu dilim)

```
dotnet/Kuyumcu.Core/Sozlesmeler/FiyatAraliklari.cs
dotnet/Kuyumcu.Api/Modules/Fiyat/FiyatModulu.cs
dotnet/Kuyumcu.Web/Bilesenler/FiyatListesiUrunPaneli.razor
dotnet/Kuyumcu.Tests/HttpFiyatAralikTestleri.cs
dotnet/Kuyumcu.Tests/FiyatListesiUrunGorunumuTestleri.cs
```
(`git status --porcelain | grep -v -E "/(bin|obj)/"` çıktısındaki diğer satırlar bu
oturumdan ÖNCE de değişikti; commit yapılmadı.)

**Dokunulan sayfalar (TEK DOKUNUŞ kaydı):** `Bilesenler/FiyatListesiUrunPaneli.razor`
(dalga 3-H, engel 20 borcu). `Pages/Fiyatlandirma.razor`'a **dokunulmadı**.

## 13. Sapmalar ve gerekçeleri

1. **Önizlemede risk onayı aranmıyor** (yalnız yazmada). Gerekçe: UX-S23 — etki
   görülmeden onay istenirse kullanıcı neyi onayladığını bilmez. Sert sınır
   önizlemede de reddedilir.
2. **Risk mesajı birleştirildi** (tek kod yerine tüm riskli alanlar). Gerekçe:
   canlı doğrulamada yanlış alanı işaret ettiği görüldü (§ 1). Birinci dilimin
   testleri bu değişiklikten sonra da yeşil.
3. **375 px kartlaştırma ve T-4a sürüm damgası uygulanmadı** — kapsam kilidi
   (talimat 13) ve tek-dokunuş kuralı: beş rotanın tablo işaretlemesine dokunmak
   bu dilimin kapsamını büyütürdü. **devredildi: S-3d** (kartlaştırma) ve
   **devredildi: T-4a** (hata kimliği/sürüm damgası).

## 14. Açık kalanlar

* Engel 20 **kurul tur 4'e AÇIK gider**: 375 px kartlaştırma (S-3d) + T-4a damgası.
* Ağaç artıkları (kurucu temizlik listesi): 16 `.mutorj`, 1 `.bak`, 8 adet 0 baytlık
  migration `.cs` — silme hook'u nedeniyle kurucuya bırakıldı.
* QA turu 4 öncesi **kuyumcu-qa (5199) yayın anlık görüntüsü yenilenmeli** (bu ajan
  porta dokunmadı).
* `Fiyatlandirma.razor`'daki liste formu POST/PATCH çift amaçlıdır; satır içi
  düzenleme kalıbı **değildir** — `SatirIciDuzenle`'ye taşıma gereği yoktur (§ 9).

## 15. Kurucuya / mali müşavire notlar

* **Eksi marj yasaklanmadı**, bilinçli onaya bağlandı: indirimli/maliyetine liste
  meşru bir ticari karardır. Sert sınırlar (marj −100…500, çarpan 0…500) mevzuat
  hükmü değil **iş güvenlik eşiğidir**; rakam değiştirilmek istenirse tek kaynak
  `Kuyumcu.Core/Sozlesmeler/FiyatAraliklari.cs`'dir.
* **Madde 1 kaydı (sır hijyeni):** doğrulama sırasında `docker-compose.yml` yapısını
  okumak için kullanılan bir `sed` maskesi satır sınırını tutturamadı ve
  **geliştirme MySQL root parolası bir kez araç çıktısına düştü** (yalnız oturum
  dökümünde; hiçbir belgeye yazılmadı). Dosya zaten "YALNIZ GELİŞTİRME" başlıklıdır
  ve parola depoda düz metindir; yine de **geliştirme parolasının döndürülmesi**
  KURUCUYA-KARARLAR md.6 (K9 rotasyonu) kapsamına eklenmelidir.
* Doğrulama için üretilen sistem anahtarı yalnız oturumun geçici dizininde kaldı ve
  API kapatıldı; kalıcı yapılandırmaya yazılmadı.
