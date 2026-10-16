# PAKET S-3d — QA turu 4 borcu (kritik/yüksek bulgular) · uygulama raporu

> Dalga 4-H · Taşkın (backend) + Ertuğ (frontend) · 2026-09-18
> Kapsam kilidi (kurucu talimatı 13): yeni ekran/modül/özellik AÇILMADI; yalnız mevcut ekranların borcu.

## 1. Durum özeti

| | Değer |
|---|---|
| Durum | **BİTTİ** (kapı şartındaki 4 öncelik maddesi) |
| Derleme | `dotnet build Kuyumcu.sln --nologo` → **0 uyarı · 0 hata** |
| Migration | **gerekmedi** — şema değişikliği yok (uçlar ve ekranlar mevcut varlıklar üzerinde) |
| Tam süit | **2834 / 2834 başarılı · 0 başarısız** (`trx-pS-3d-kapi2`) |
| Denetim | `dotnet/denetim.sh` → **DENETİM TEMİZ** |
| Mutasyon | **9 mutasyon**, hepsi ilgili testi kırmızıya çevirdi, hepsi `cmp` ile geri alındı |
| Kiracı | başlangıç 8 → bitiş **8** (kendi test kiracım + önceki yarım koşunun kiracısı silindi) |
| Commit | **YOK** (kırmızı çizgi) |

## 2. Devralma notu (Anayasa md.1 — gizlenmiyor)

Bu oturum S-3d'yi **sıfırdan başlatmadı**. Aynı iş için 05:00–05:56 arasında yarım kalan
bir koşu vardı: `S3dEkranTestleri.cs` / `S3dHttpTestleri.cs` ve dört öncelik maddesinin
kodu yazılmış, tam süit 05:56'da yeşil kapanmış, ama **rapor, mutasyon kanıtı, canlı
doğrulama ve temizlik yapılmamıştı**. Ayrıca veritabanında o koşunun test kiracısı
(`PAKETS-3d-TEST Kuyumcu`, `01M2S769GNHN7AK8WBSYQEKZDG`) duruyordu.

Bu oturumda yapılanlar: kodun spesifikasyona karşı doğrulanması, **9 mutasyon kanıtı**,
tam süitte ortaya çıkan **gerçek bir sıra bağımlılığı hatasının düzeltilmesi**, 5198
üzerinde canlı doğrulama, iki test kiracısının da silinmesi ve bu rapor.

## 3. Kapatılan maddeler (kapı şartı öncelik sırası)

### DP4-01 · `/islemler` reyon çıkmazı (KRİTİK)
- `Islemler.razor`: tek reyon varken de `<select id="islem-reyon">` çizilir ve **ilk seçenek `—`**'dir.
  Tek reyon bilgisi `#islem-reyon-tek` rozetiyle korunur (UX-S22 bilgisi kaybolmaz).
- `SecilenReyon` artık **otomatik atama yapmaz**: kullanıcı boş bırakırsa gövdeye `reyonId` konmaz,
  sunucu "önce atanmamış mal" sırasını işletir.
- Gerekçe (kodda da yazılı): UX-S25 (çıkmazsızlık) UX-S22'ye (tek seçenekte seçiciyi gizle) **üstün geldi** —
  seçiciyi gizlemek satışı tümden kilitliyordu.

### DP4-02 · `/uyum` "Kapat" akışı (KRİTİK)
- `UyumServisi.Kapat` gövdesi `kapatmaGerekcesi` → **`gerekce`** (sunucudaki `KapatDto.Gerekce` sözleşmesi).
- Sözleşme testi eklendi: gövdede `gerekce` **olur**, `kapatmaGerekcesi` **olmaz**.

### DP4-03 · `/gider-pusulalari` hurda alış satıcı kimliği (KRİTİK)
- `Islemler.razor` "Hurda Alış" formuna `#hurda-satici` alan kümesi: satıcı tipi
  (NIHAI_TUKETICI / ESNAF_MUAF / MUKELLEF), ad-soyad, TCKN, adres; MUKELLEF'te TCKN yerine
  **tedarikçi fatura no** istenir.
- Cari seçiliyse ad **cariden ön dolar** ve `#hurda-satici-ad-kaynak` rozetiyle kaynağı yazılır (UX-S21).
- Alan altı Türkçe mesajlar + `#islem-kaydet-kilit` kilit sebebi (UX-S30, DoD EK md.3):
  eksikken **istek hiç atılmaz** (eskiden POST gidip 400 dönüyor, sebep görünmüyordu).
- Sunucu tarafı zaten fail-closed'dı (`HurdaAlisDto.Satici` null → 400); değişen, ekranın sormaması.

### DP4-04 · Gider pusulası düzenlenebilirliği (KRİTİK)
- Yeni uç: `PATCH /api/v1/gider-pusulalari/{id}` — `[RolGerekli("SAHIP","MUDUR")]`.
- **Yalnız mali kimlik dışı alanlar**: `saticiAdres`, `malAciklamasi`. Satıcı adı/TCKN/tutar/stopaj
  VUK md.234 gereği kilitli kalır; ekranda gerekçe + doğru yol (İPTAL + YENİ PUSULA) yazılı.
- PATCH sözleşmesi: **gönderilmeyen alan KORUNUR, `null` SİLİNİR**. Düzenlenebilir alan hiç
  gönderilmezse 400 + doğru yol metni. İptal edilmiş pusula → 409.
- Denetim izi: `DenetimServisi.YazmaKaydet` çağrılır, `hassas: true`, **değer yazılmaz** — yalnız
  hangi alanın değiştiği.

## 4. DoD sözleşmesi

| Varlık / kayıt | Düzenleme yolu | Yetki | Denetim | Gerekçe (salt okunursa) |
|---|---|---|---|---|
| Gider pusulası — adres, mal açıklaması | `PATCH /gider-pusulalari/{id}` + ekranda Düzenle | SAHIP, MUDUR | YazmaKaydet, hassas | — |
| Gider pusulası — satıcı adı, TCKN, tutar, stopaj, gram, ayar | **salt okunur** | — | — | VUK md.234 belge bütünlüğü; doğru yol: **İPTAL + YENİ PUSULA** (ekranda `DegismezIpucu`) |
| Gider pusulası — belgenin tamamı | `POST /gider-pusulalari/{id}/iptal` (gerekçe ≥ 20 karakter) | SAHIP, MUDUR | var | hard DELETE **yok** (finansal varlık) |
| MASAK şüpheli bildirimi | `PATCH /masak/supheli/{id}/kapat` (gövde alanı **`gerekce`**) | SAHIP, MUDUR | var | kayıt **silinmez** — 8 yıl saklama |
| İşlem satırındaki reyon | satış/alış/takas formunda seçici (`—` dâhil) | işlem yetkisi | işlem kaydı | — |

Finansal varlıklarda hard DELETE açılmadı; yeni varlık/tablo doğmadı (migration gerekmedi).

## 5. Sapmalar ve gerekçeleri

1. **Kapsam**: QA turu 4 düzeltme listesi 21 madde (DP4-01…DP4-21). Bu paket kapı şartındaki
   **4 öncelik maddesini** kapattı. Kalan 17 madde kapsamı büyüttüğü için — kapsam kilidi
   gereği — **devredildi** (§ 9).
2. **UX-S22 ihlali bilinçli**: tek reyon varken seçici artık gizlenmiyor. Gerekçe § 3/DP4-01.
3. **Migration yok**: spesifikasyon şema değişikliği istemedi; `GiderPusulalari` ve
   `MasakSupheliBildirimler` varlıkları mevcut.
4. **Sistem anahtarı**: canlı doğrulama için `Sistem__Anahtari` ortam değişkeni ile geçici bir
   anahtar üretildi (appsettings'teki değer `DEGISTIR-` ön ekli yer tutucu). Değer hiçbir çıktıya
   yazılmadı, yalnız 5198 sürecinin ömrü boyunca kullanıldı. **Kaynak dosya değiştirilmedi.**

## 6. Tam süitte yakalanan gerçek hata (mutasyonla değil, süitle)

`S3dEkranTestleri.Satici_tipi_secilmeden_Kaydet_KILITLI_ve_sebebi_yazili` **filtreli koşumda
yeşil, tam süitte kırmızıydı**:

```
Assert.Contains() Failure: Sub-string not found
String:    "Cari seçin"
Not found: "Satıcı kimliği eksik"
```

Sebep: `Ac()` yardımcısı `#islem-cari` **seçicisini** bekliyordu ama sahte API'den gelen
**seçeneklerin** dolmasını beklemiyordu. Yük altında `Change("c1")` boşa düşüyor, kilit sebebi
"Cari seçin"e kayıyordu. Düzeltme: `Ac()` artık `c1` seçeneğinin varlığını `WaitForAssertion`
ile bekliyor. Düzeltmeden sonra mutasyon (M-S3d-04b) tekrarlandı — test hâlâ kırmızıya dönüyor,
yani sertleştirme testi zayıflatmadı.

## 7. Mutasyon tablosu

| Kod | Dosya | Değişiklik | Kırmızıya dönen test(ler) | Geri alma |
|---|---|---|---|---|
| M-S3d-01 | `Islemler.razor` | `SecilenReyon` tek reyonu yeniden otomatik atasın | `Reyon_secilmemisse_govdeye_OTOMATIK_reyon_konmaz` | cmp=AYNI |
| M-S3d-02 | `Islemler.razor` | seçici koşulu `Count > 0` → `Count > 1` (tek reyonda gizlen) | `Tek_reyon_varken_de_secici_cizilir_ve_bos_secenek_vardir`, `Reyon_secilmemisse_govdeye_OTOMATIK_reyon_konmaz` | cmp=AYNI |
| M-S3d-03c | `UyumServisi.cs` | kapat gövdesi `gerekce` → `kapatmaGerekcesi` (tek eşleşme) | `Kapat_govdesinde_alan_adi_gerekce_olur_kapatmaGerekcesi_OLMAZ` | cmp=AYNI |
| M-S3d-04 | `Islemler.razor` | `HurdaSaticiTipiHata` hep null | `Satici_tipi_secilmeden_Kaydet_KILITLI_ve_sebebi_yazili` | cmp=AYNI |
| M-S3d-04b | `Islemler.razor` | aynı mutasyon, **sertleştirilmiş testle** tekrar | `Satici_tipi_secilmeden_Kaydet_KILITLI_ve_sebebi_yazili` | cmp=AYNI |
| M-S3d-05 | `Islemler.razor` | TCKN 11 hane kontrolü kaldırıldı | `TCKN_11_hane_degilse_alan_alti_mesaj_cikar_ve_Kaydet_kilitli_kalir` | cmp=AYNI |
| M-S3d-06 | `HurdaModulu.cs` | PATCH ucundan `[RolGerekli]` kaldırıldı | `MUHASEBE_pusula_duzenleyemez_403` | cmp=AYNI |
| M-S3d-07 | `HurdaModulu.cs` | iptal edilmiş pusula kilidi devre dışı | `Iptal_edilmis_pusula_duzenlenemez_409` | cmp=AYNI |
| M-S3d-08 | `HurdaModulu.cs` | `Alan()` eksik alanı "var" saysın (PATCH semantiği bozulur) | `PATCH_sozlesmesi_gonderilmeyen_alan_KORUNUR_null_SILINIR`, `Duzenlenebilir_alan_gonderilmezse_400_ve_dogru_yol_yazilir`, `Pusula_duzenlemesi_denetim_izine_yazilir_deger_yazilmadan` | cmp=AYNI |

**Test açığı bulunmadı** — her mutasyon en az bir testi kırdı.
M-S3d-03'ün ilk denemesi iki yerde eşleşti (PusulaIptal da `new { gerekce }` kullanıyor);
tek eşleşmeli hâli M-S3d-03c olarak tekrarlandı.

## 8. Canlı doğrulama (localhost:5198, kiracı `PAKETS-3d-TEST Kuyumculuk`)

| # | Adım | Beklenen | Gerçek |
|---|---|---|---|
| 1 | `POST /masak/supheli` (gerekçe ≥ 300 karakter) | 201 | **201** — id `01M2S7WTBCPHMBRWEDV5ECCAZJ` |
| 2 | `PATCH /masak/supheli/{id}/kapat` gövde **`kapatmaGerekcesi`** (QA'daki hata) | 400 | **400** `{"alanlar":{"gerekce":["Gerekçe zorunludur"]}}` |
| 3 | `PATCH .../kapat` gövde **`gerekce`** (düzeltme) | 200 | **200** `durum: KAPATILDI` |
| 4 | `POST /islemler/hurda-alis` satıcı bloğu **yok** | 400 | **400** "Hurda alışında satıcı kimliği zorunludur (VUK md.234…)" |
| 5 | `POST /islemler/hurda-alis` satıcı bloğu **var** | 200 | **200** hasGrami 2,748 · ödenen 18.686,40 TL · pusula `GP-2026-000001` |
| 6 | `PATCH /gider-pusulalari/{id}` `{"saticiAdres":"…"}` | 200, `malAciklamasi` korunur | **200** — adres güncellendi, `malAciklamasi` = "Hurda 22 ayar bilezik" **korundu** |
| 7 | `PATCH /gider-pusulalari/{id}` `{}` | 400 + doğru yol | **400** "Düzenlenebilir alan gönderilmedi … İPTAL + YENİ PUSULA yolunu kullanın" |
| 8 | `PATCH /gider-pusulalari/{id}` `{"malAciklamasi":null}` | 200, alan silinir, adres korunur | **200** — `malAciklamasi` = NULL, adres korundu |
| 9 | `POST /islemler/satis` **boş reyon zorlanmış** (eski ekran davranışı) | 400 (çıkmazın kanıtı) | **400** "'PAKETS-3d-TEST Vitrin' reyonunda yeterli mal yok…" |
| 10 | `POST /islemler/satis` **reyon gönderilmeden** (yeni ekran) | 200 | **200** — hareket `01M2S7YXGFESMSP0140BQ9BZMY` |

### SQL kanıtı

```
hareket_toplam            3
satis                     1     ← reyonsuz satış deftere yazıldı
gider_pusulasi            1
supheli_KAPATILDI         1
pusula_adres = 'YENI ADRES — S3d duzeltme' | mal_aciklamasi = NULL
```

Denetim izi (`denetim_kayitlari`, varlık `GiderPusulasi`, `hassas = 1`):

```
gider pusulası GP-2026-000001 düzenlendi · değişen alan: satıcı adresi
gider pusulası GP-2026-000001 düzenlendi · değişen alan: mal açıklaması
```

Kişisel veri (ad, TCKN, adresin kendisi) **yazılmadı** — yalnız alan adı.

### Temizlik
- 5198 süreci durduruldu → `lsof -nP -iTCP:5198 -sTCP:LISTEN` **boş**.
- Kendi test kiracım (`01M2S7W83GWV1JRT1EM2HMESV1`) ve önceki yarım koşunun kiracısı
  (`01M2S769GNHN7AK8WBSYQEKZDG`) `tenant_id` kolonu olan 58 tablodan silindi.
- `SELECT COUNT(*) FROM tenants` → **8** (QA turu 4'ün kayda geçen taban değeri).
- `QA-TEST-1/2/3` fikstür kiracılarına **dokunulmadı** (QA-TEST-3'te 10 hareket duruyor).

## 9. Devredilenler (kapsam kilidi gereği açılmadı)

QA turu 4 düzeltme listesinin kalan 17 maddesi — hepsi **devredildi: S-3e (ve sonrası)**:
DP4-05 düzenlenebilirlik gerilemeleri (`/cariler` Pasife Al, `/urunler/agac` kategori,
`/faturalar` TASLAK düzenleme, `/kullanicilar` SAHİP devri) · DP4-06 doğrulama kalıbı
(`/sayim` Enter=kaydet, zorunlu alan işareti) · DP4-07 `/kur` dayanıklılığı · DP4-08 dar ekran ·
DP4-09 karanlık tema + yapışkan rol sütunu · DP4-10 `/sayim` DURUM rozeti ·
DP4-11 `/denetim-izi` yarış durumu · DP4-12…DP4-21.

**Not — DP4-04'ün "para hatası" ayağı devredildi:** `/vezne` kuyruk toplamının ACIK fişlerde
0 ₺ gelmesi ve `/kurulum` negatif açılış bakiyesi kapısı bu pakette **kapatılmadı**;
öncelik sırası dört maddeyle sınırlıydı. Vezne personelinin 0 ₺ görüp yanlış tahsilat
yapma riski **açık duruyor** — sıradaki dalgada ilk sıraya alınmalı.

## 10. Koşul karşılama

| Koşul | Durum | Kanıt |
|---|---|---|
| Hatayı yeniden üreten test | ✔ | 18 yeni test (9 bUnit + 9 HTTP/sözleşme) |
| Mutasyon kanıtı | ✔ | § 7 — 9 mutasyon, hepsi kırmızı, hepsi cmp=AYNI |
| Canlı doğrulama (5198) | ✔ | § 8 — 10 adım, HTTP kodları + SQL |
| `dotnet/denetim.sh` TEMİZ | ✔ | "DENETİM TEMİZ" |
| Tam süit 0 başarısız | ✔ | 2834/2834 |
| Test kiracısı silindi | ✔ | tenants 8, QA-TEST fikstürleri korundu |
| Tip sözleşmesi kuralı | ✔ | PATCH yanıt modeli `PusulaDetay` (`UyumServisi.cs:259-265`) canlı gövdeye karşı doğrulandı: `gramAgirlik 3.0` · `hasGrami 2.748` · `brutTutar 18686.4` · `stopajOran 0.0` · `stopajTutar 0.0` · `netOdenen 18686.4` hepsi **decimal**, `saticiAdres`/`malAciklamasi` string?, `iptalEdildi` bool — istemcide int YOK |
| Yeni istemci record yasağı | ✔ | yeni record açılmadı (denetim.sh tavanı 122 sabit) |
| PATCH semantiği | ✔ | gönderilmeyen alan korunur / null siler — test + canlı |
| COMMIT YOK | ✔ | git komutu çalıştırılmadı |
| 5199'a dokunulmadı | ✔ | yalnız 5198 kullanıldı |

## 11. Kurucuya / mali müşavire notlar

1. **VUK md.234 yorumu — DOĞRULANMALI.** Gider pusulasında hangi alanların belge düzenlendikten
   sonra düzeltilebileceği koda **tutucu** girildi: yalnız satıcı adresi ve mal açıklaması.
   Mali müşavir "adres de kilitli olmalı" derse uç daraltılır; "teslim tarihi de düzeltilebilir"
   derse genişletilir. **Mevzuat hükmü koda sabit yazılmadı, tutucu tarafta bırakıldı.**
2. **Reyon kararı.** Tek reyon varken seçici artık gizlenmiyor (UX-S22'den sapma). Kuyumcu
   "tek vitrinim var, her seferinde seçmek istemiyorum" derse çözüm otomatik atama değil,
   **reyona mal yerleştirme akışıdır**; aksi hâlde çıkmaz geri döner.
3. **Açık risk:** `/vezne` kuyruğunda ACIK fişlerin toplamı hâlâ 0 ₺ görünüyor (§ 9). Ürün
   "satışa hazır" sayılmadan önce kapatılmalı.

## 12. Değişen dosyalar (S-3d ayak izi)

Depo tek commit'li (`bbfdc95`) olduğu için `git status` tüm ağacı değişmiş gösterir;
aşağıdaki liste **dosya değişim zamanlarına** göre S-3d'ye ait olanlardır:

**Üretim kodu**
- `dotnet/Kuyumcu.Web/Pages/Islemler.razor` — reyon seçici + otomatik atama kaldırma (DP4-01); hurda satıcı kimliği bloğu ve doğrulaması (DP4-03)
- `dotnet/Kuyumcu.Web/Servisler/UyumServisi.cs` — `Kapat` gövde alan adı (DP4-02); `PusulaGuncelle` istemci ucu (DP4-04)
- `dotnet/Kuyumcu.Api/Modules/Hurda/HurdaModulu.cs` — `PATCH /gider-pusulalari/{id}` ucu + servis (DP4-04)
- `dotnet/Kuyumcu.Web/Pages/GiderPusulalari.razor` — detayda düzenleme yolu / `DegismezIpucu`
- `dotnet/Kuyumcu.Core/I18n/Sozlukler.cs` — yeni TR + EN anahtarlar (satıcı tipi, TCKN, kilit sebepleri, reyon ipucu)

**Test kodu (yeni)**
- `dotnet/Kuyumcu.Tests/S3dEkranTestleri.cs` — 9 bUnit testi (+ bu oturumda `Ac()` sertleştirmesi, § 6)
- `dotnet/Kuyumcu.Tests/S3dHttpTestleri.cs` — 9 HTTP/sözleşme testi

**Dokunulan sayfalar (DoD EK 2 — tek dokunuş kaydı):** `Islemler.razor`, `GiderPusulalari.razor`.
Her ikisi de bu dalgada **ilk kez** S-3d kapsamında açıldı.

## 13. Açık kalanlar

1. **QA turu 4'ün 17 maddesi** kapsam kilidi gereği devredildi (§ 9).
2. **`/vezne` ACIK fiş toplamı 0 ₺** — para hatası riski, açık (§ 9 notu).
3. **Depo artıkları (bana ait DEĞİL):** `dotnet/` altında 15–17 Eylül tarihli 16 adet `*.mutorj`
   ve 1 adet `*.bak` duruyor. TurkReport hook `rm`/`mv`'yi engellediği için **silinmedi**;
   temizliği kurucuya bırakıldı. Bu oturumda 06:00 sonrası **hiç** yeni artık üretilmedi (doğrulandı).
4. **`denetim_kayitlari.eylem` alanı** pusula düzenlemesinde `EKLEME` yazıyor (ayrıca EF
   dinleyicisinden `GUNCELLEME` satırı da düşüyor). `DenetimServisi.YazmaKaydet` ortak yardımcı
   olduğu ve "yeniden yazılmaz" kuralı bulunduğu için **dokunulmadı**; eylem etiketi
   tutarlılığı ayrı bir paket işidir.
