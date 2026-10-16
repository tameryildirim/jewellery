# PAKET P-36c (İKİNCİ TUR) — SELECT GÖÇÜNÜN BİTİRİLMESİ + PENCERE TEK TİPİ
## Kuyumcu ERP · Dalga 6 · 2026-09-21 · Taşkın (backend) + Ertuğ (frontend) rolü

> **DURUM: YARIM.** Paketin **birinci ayağı (select göçü) BİTTİ** — serbest
> ekranlarda ham `<select>` **0**'dır ve bu fail-closed bir sayaçla kilitlendi.
> Pencere ayağının **davranış ve tutarlılık kısmı** (düğme sırası, boş durum,
> pencere boyu, çıplak `catch`) bitti ve sayaçlandı; **46 rota × 3 çözünürlük
> ölçümü koşuldu ve taşma 0**. **Ham girdi göçü (HAM_INPUT → 0) HİÇ
> BAŞLANMADI** ve **QA turu / kabul eşiği ÖLÇÜLMEDİ.** Anayasa md.1 gereği
> bu rapor "bitti" demiyor; ne yapılmadığı § 9'da adıyla yazılıdır.

Önceki tur raporu: `PAKET-P-36c-uygulama-raporu.md` (o da `YARIM` demişti).
Bu rapor onun ÜSTÜNE koyar; orada biteni tekrar etmez.

---

## § 0. ÖNCE ÖLÇÜLDÜ (belgeden kopyalanmadı)

| Sayaç | Bu tur ÖNCESİ | Bu tur SONRASI | Ölçüm |
|---|--:|--:|---|
| HAM_SELECT (tüm ağaç, denetim.sh komutu) | 85 | **30** | `grep -rho '<select[ >]' … --exclude='Secici.razor'` |
| — bunun serbest ekranlardaki kısmı | 58 | **0** | yorumlar ayıklanmış sayım (aşağıda) |
| — bunun kilitli ekranlardaki kısmı | 26 | 26 | kapsam dışı, kendi paketlerine devredildi |
| `<Secici>` kullanımı | 97 | **155** | `grep -rho '<Secici[ >]'` |
| HAM_INPUT | 357 | **357** | DEĞİŞMEDİ — bu ayak yapılmadı |
| Test | 3617 | **3624** | tam süit |
| Derleme | 0 hata / 1 uyarı | **0 hata / 0 uyarı** | `dotnet build Kuyumcu.sln --nologo` |

**Ölçüm dürüstlüğü notu:** ham `grep '<select'` ağaçta 30 sayıyor; bunun
**26'sı kilitli ekranlarda GERÇEK `<select>` elemanı**, **4'ü yorum metninde**
geçen "`<select>`" sözcüğüdür (Secici.razor, SubeSecici.razor, Islemler.razor,
Vezne.razor, Panel.razor açıklamaları). Yorum metnini sayaçtan kaçmak için
değiştirmedim — bu yalan olurdu; onun yerine **yeni sayaç yorumları ayıklıyor**
(`@* *@` ve `//`), çünkü bir yorum denetim öğesi değildir.

---

## § 1. YAPILAN 1 — SELECT GÖÇÜNÜN TAMAMLANMASI (58 → 0)

21 serbest ekranda kalan **58 ham `<select>`** ortak `<Secici>` bileşenine
geçirildi. Önceki turun otomatik dönüştürücüsünün atladıkları bunlardı ve
hepsinin bir sebebi vardı:

| Atlanma sebebi | Adet | Çözüm |
|---|--:|---|
| `<label for>` yok, yalnız `aria-label` (süzgeç çubukları) | 6 | Etiket görünür hâle geldi, yeni kimlik `<önek>-<alan>-filtre` |
| `disabled="@(...)"` ifadesi | 12 | `Devredisi` + **`DevredisiSebebi`** (UX-S30: kilit sebebi artık YAZILI) |
| Dinamik kimlik `id="@($"...-{i}")"` | 8 | `ElemanId="@($"...")"` birebir korundu |
| `value=` + `@onchange` (ChangeEventArgs imzalı) | 11 | `Deger` + `DegerChanged`; **iş yolu yeniden yazılmadı**, sarmalandı |
| Seçenek metninde iç içe çift tırnak / `{ }` deseni | 21 | Seçenek dizisi `@code` tarafına alındı (Razor öznitelik ayrıştırıcısı bozuluyordu) |

**Dokunulan sayfalar (DoD EK 2 — TEK DOKUNUŞ):**
`Banka · BankaEkstre · Cariler · DenetimIzi · Evraklar · Gider · Hurda ·
IslemDefteri · Islemler · Isletme · Kasalar · Kullanicilar · Kur · Kurulum ·
PosTahsilatlar · UrunAgaci · Urunler · Uyum · VeriIceAktarim · Vezne · Vitrin`
ve bileşenler `ModalPencere · Cekmece`.
Bunların çoğu P-36c birinci turunda da açılmıştı; **gerekçe: aynı paketin aynı
işi, birinci tur zaman yetmediği için YARIM bırakmıştı** (kapı kaydına yazılı).
**Kilitli ekranlara DOKUNULMADI.**

### 1.1 Göçün ortaya çıkardığı ve kapatılan kusurlar

1. **Kilitli seçicinin sebebi yoktu.** `disabled` 12 yerde sessizdi; artık her
   birinde `DevredisiSebebi` var (ör. "Bu araç için tek uygun kasa var; kasa
   otomatik seçildi."). UX-S30 bu pakette gerçekten uygulandı.
2. **Ham `<select disabled>` testte hâlâ değer kabul ediyordu.** bUnit'te
   `.Change()` kilitli select'e de yazıyordu; yani "kilitli" iddiası test
   tarafında hiç sınanmıyordu. `SeciciYardim.Sec` artık kilitli seçicide
   istenen değer **zaten seçiliyse no-op**, **değilse kilit sebebiyle KIRILIR**.
   (Bu açık mutasyon R7 ile ölçüldü — aşağıya bakın.)
3. **`u-ayar` / `d-ayar` seçenek metni** Razor özniteliği içinde
   `ToString("0.##", …)` çağırıyordu; iç içe çift tırnak ayrıştırıcıyı bozuyor.
   Tek kaynak `AyarSecenekleri(maden)` yardımcısına taşındı — oluşturma ve
   düzenleme formu artık **aynı diziyi** kullanıyor (UX-S26).

### 1.2 Sözlük (TR + EN, çıplak metin yok)

Yeni anahtarlar: `Ortak.Durum`, `Ortak.Kaydediliyor`, `Islem.BacakArac`,
`Islem.BacakTaksit`, `Islem.BacakKasaOtomatik`, `Islem.BacakPosTek`,
`Evrak.TurDegismez`, `Gider.VirmanHedef`, `UrunAgaci.ReyonSubeDegismez`,
`Yonetim.SaltOkunurKilit`. Hepsi iki dilde; eksik anahtar derleme hatası verir.

---

## § 2. YAPILAN 2 — PENCERELERİN TEK TİPE İNMESİ (ölçülen kısım)

### 2.1 Düğme sırası — ÖLÇÜLDÜ, sonra karar verildi

Varsayım yapmadım, saydım:

| Sıra | Pencere sayısı |
|---|--:|
| `İptal → Kaydet` (ikincil önce) | **20** |
| yalnız ikincil | 14 |
| yalnız birincil | 2 |
| `Kaydet → İptal` (birincil önce) | **1 modal + Çekmece'nin KENDİSİ + 3 Uyum penceresi** |

Standart **ölçülen çoğunluk** olarak seçildi: **ikincil önce, birincil sonda.**
`Cekmece.razor` (tüm çekmeceler), `Uyum.razor` (3 pencere) ve
`VeriIceAktarim.razor` (1 pencere) buna çekildi. **Eleman kimlikleri
değişmedi** (`cekmece-*-kaydet` / `-iptal`), dolayısıyla hiçbir test kimlik
yüzünden kırılmadı. Sayaç: `PENCERE_DUGME_SIRASI_TERS = 0`.

### 2.2 Pencere boyu

`.modal-panel` ve `.cekmece` üzerinde **2 boy** var (modal `32rem`, çekmece
`min(30rem,100vw)`); tam ekran mobil geri dönüşleri (`none`, `100vw`) boy
sayılmaz. Tavan 3 olarak kilitlendi. **Yeni boy sınıfı AÇMADIM** — hiçbir sayfa
istemiyordu, açmak kapsam büyütmek olurdu.
⚠ İlk yazdığım ölçüm `.cekmece-kapat`ı (bir düğme) pencere sanıp 3 diyordu;
regex `(?![\w-])` ile daraltıldı ve gerçek değer **2** çıktı. Yanlış olan değil,
ÖLÇÜLEN yazıldı.

### 2.3 Çıplak `catch { }` (doğrulayıcı bulgusu)

Doğrulayıcı "rapor 'catch{} yutması kalktı' diyor, kalkan tek bir yol" demişti.
Doğruydu. `ModalPencere.razor`da 3, `Cekmece.razor`da 2 çıplak yakalama daha
vardı; hepsi `catch (JSDisconnectedException)` + `catch (JSException)` biçimine
daraltıldı ve **kaynak testi** ile kilitlendi (`C36c2_5`).

---

## § 3. YAPILAN 3 — CIRCIR SAYAÇLARI (doğrulayıcının "HİÇ YOK" dediği sayaçlar)

`dotnet/denetim.sh`e eklendi (hepsi fail-closed):

```
✓ P-36c: HAM_SELECT_SERBEST        0 (serbest ekranlarda ham select yok)
✓ P-36c: PENCERE_DUGME_SIRASI_TERS 0 (ikincil önce, birincil sonda)
✓ P-36c: PENCERE_GENISLIK_CESIDI   2 (tavan 3)
✓ P-36c: BOS_DURUM_METINSIZ        0 (her boş durum metinli)
```

Sıkılaştırılan mevcut cırcırlar:

| Sayaç | Eski | Yeni | Gerekçe |
|---|--:|--:|---|
| `P36_SELECT_TAVAN` | 84 | **30** | taze ölçüm |
| `P36C_GOC_TABAN` | 92 | **155** | doğrulayıcı bulgusu: taban ölçülenin ALTINDAYDI (92 < 97), 5 birimlik geri dönüş boşluğu vardı |

`SABIT_RENK` tavanı (14) **düşürülmedi**: bu turda renk sabiti eklenmedi ama
silinmedi de; düşürmek için ölçülmüş bir azalma yok (md.1 — uydurmuyorum).

---

## § 4. DERLEME

```
dotnet build Kuyumcu.sln --nologo
    0 Uyarı
    0 Hata
```
Devralınan tek uyarı (`P36cGocTestleri.cs` xUnit2013 `Assert.Equal(1, …Count)`)
`Assert.Single`e çevrildi; ağaç artık **0 uyarı**.

## § 5. MIGRATION

**Gerekmedi.** Bu paket yalnız arayüz + test + denetim betiği değiştirir; hiçbir
varlık, kolon veya sözleşme alanı eklenmedi. `kuyumcu_net`te migration sayısı
değişmedi.

---

## § 6. TESTLER

| Koşum | Toplam | Başarılı | Başarısız |
|---|--:|--:|--:|
| Paket öncesi (taban, tek koşu) | 3617 | 3617 | 0 |
| Göç sonrası ilk koşu (ham) | 3617 | 3536 | **81** |
| Test göçü 1. tur sonrası | 3617 | 3588 | 29 |
| Test göçü 2. tur sonrası | 3617 | 3617 | 0 |
| **SON (tek koşu, trx kanıtlı)** | **3624** | **3624** | **0** |

Son koşum kanıtı (trx `Counters`):
`total="3624" executed="3624" passed="3624" failed="0" error="0"`
Dosya: `scratchpad/trx-pP-36c2/_Ramazan-MacBook-Air_2026-09-21_14_23_29.trx`

**Yeni test: 7** (`C36c2_1…C36c2_5` + `SeciciYardimKapisiTestleri`nin 2 testi).

### 6.1 81 kırmızı NEDEN doğdu ve nasıl kapandı (göç hatası DEĞİL)

Göçün sözleşmesi "kimlik korunur, **eleman türü değişir**". Türü değişince
`Find("#x").Change(v)` geçersiz olur — 59 çağrı `SeciciYardim.Sec`e,
9 `GetAttribute("value")` okuması yeni `SeciciYardim.SeciliDeger`e,
`InnerHtml`/`option[value=]` okumaları `ListeMetni`/`Secenekler`e çevrildi.
**Tek kapı korundu:** ikinci bir "seçici tıklama" kalıbı açılmadı.

Üç test **davranış olarak** yeniden yazıldı, çünkü kilitli seçicide "seçenek
listesini say" artık anlamsız (liste açılmıyor):
`DovizBacagi.Kasa_listesi_…_FILTRELENIR`, `UX4b.Tahsilat_kasa_secicisinde_DOVIZ_kasasi_YOKTUR`,
`Urun_grubu_ve_ayar_listeleri_TEK_KAYNAKTAN_gelir`. Yeni iddia daha güçlü:
**seçili değer + kilit + kilit sebebi** (UX-S22 + UX-S30).

### 6.2 Doğrulayıcının bildirdiği 2 kararsız test

`AlisOdemeAraci…SATISTAN_ALISA…` (gerçekte `PosBarkodTestleri` içinde) ve
`P16…Birim_fiyat_sunucudan_DOLDURULUR`. İkincisine açık
`WaitForAssertion(..., 5 sn)` kondu; `UrunEkranTestleri.Maden_secilince…`
testine de modal açılışı için `WaitForElement` eklendi. Son üç tam süit
koşumunda üçü de yeşil. **Kararsızlık "belgelenip geçilmedi", yarış kaldırıldı.**

---

## § 7. MUTASYON KANITLARI

⚠ **Önce bir ölçüm kusuru buldum ve düzelttim:** ilk mutasyon koşucum geri
alımda `shutil.copy2` kullanıyordu; bu **zaman damgasını da geri aldığı için**
MSBuild yeniden derlemiyor, **mutasyon derlenmiş DLL'de kalıyordu**. Kaynakta
`cmp` birebir eşleşiyor ama koşan kod mutantti. Bunu tam süitteki tek kırmızıyı
kovalarken yakaladım (`id="u-ayar-yeni"` render ediliyordu). Koşucu düzeltildi
(`copy` + `os.utime`) ve **yedi mutasyonun HEPSİ baştan koşuldu**. Aşağıdaki
tablo düzeltilmiş koşumundur.

| Kod | Değişiklik | Kırmızıya dönen test | Geri alındı (cmp) |
|---|---|---|---|
| R1 | `Kasalar.razor`: `<Secici kasa-doviz>` yerine ham `<select>` | `C36c2_1_Serbest_ekranlarda_ham_select_KALMAZ` | birebir |
| R2 | `Cekmece.razor`: düğme sırası eskiye (birincil önde) | `C36c2_2_Pencere_dugme_sirasi_TEK_TIPTIR` | birebir |
| R3 | `Kasalar.razor`: `<BosDurum Mesaj=…>` → `<BosDurum />` | `C36c2_3_Bos_durum_METINSIZ_olamaz` | birebir |
| R4 | `app.css`: 3 yeni pencere genişliği | `C36c2_4_Pencere_genislik_cesidi_EN_COK_UCTUR` | birebir |
| R5 | `Cekmece.razor`: `catch (JSException)` → `catch { }` | `C36c2_5_Pencere_bilesenlerinde_CIPLAK_catch_kalmaz` | birebir |
| R6 | `Urunler.razor`: `ElemanId="u-ayar"` → `"u-ayar-yeni"` | `UrunEkranTestleri.Maden_secilince_…` + `Urun_grubu_madeni_dayatiyorsa_…` | birebir |
| R7 | `SeciciYardim.cs`: kilitli seçici koruması kaldırıldı | `SeciciYardimKapisiTestleri.Kilitli_secicide_BASKA_deger_istenirse_test_SESSIZCE_GECMEZ` | birebir |

**Bulunan TEST AÇIĞI:** R7 ilk denemede **hiçbir testi kırmadı** (27 test yeşil
kaldı) — yani kilitli bir seçiciye yanlış değer "atanmış gibi" sessizce
geçilebiliyordu. Açık kapatıldı (`SeciciYardimKapisiTestleri` eklendi) ve
mutasyon tekrarlandı; artık kırmızıya dönüyor.

Son mutasyondan sonra temiz derleme: **0 uyarı / 0 hata.**

---

## § 8. DENETİM VE CANLI DOĞRULAMA

### 8.1 `dotnet/denetim.sh`
```
✓ P-36 : HAM_SELECT                 30  (tavan 30, yalnız azalır)
✓ P-36 : HAM_INPUT                 357  (tavan 357, yalnız azalır)  ← DEĞİŞMEDİ
✓ P-36c: SECICI_GOC                155  (taban 155, yalnız artar)
✓ P-36c: ODAK_TUZAGI                 0
✓ P-36c: PENCERE_KAYDIRMA_KILIDI     1
✓ P-36c: SECICI_TEST_TEK_KAPI        1
✓ P-36c: HAM_SELECT_SERBEST          0
✓ P-36c: PENCERE_DUGME_SIRASI_TERS   0
✓ P-36c: PENCERE_GENISLIK_CESIDI     2  (tavan 3)
✓ P-36c: BOS_DURUM_METINSIZ          0
DENETİM TEMİZ.
```

### 8.2 Canlı (kendi sunucum: 5198 — 5199'a DOKUNULMADI)

| Adım | Beklenen | Gerçek | Kanıt |
|---|---|---|---|
| API açılışı | 200 | **200** `Healthy` | `GET /saglik` |
| Kabuk servisi | 200, Blazor kabuğu | **200** | `GET /` |
| `app.css` yayınlanıyor | 200, `.modal-alt` içerir | **200**, 1 eşleşme | `GET /css/app.css` |
| Giriş (QA fikstürü) | 200 + jeton | **200**, rol `SAHIP` | `POST /api/v1/auth/giris` |
| Kur kaynakları | 200 + 3 kaynak | **200** `["ALTINKAYNAK","ANKARA","TRUNCGIL"]` | `GET /api/v1/kur/kaynaklar` |
| Kasalar / Ürünler / Cari / Kategoriler / Reyonlar | 200 | **200** (boş fikstür, `[]`) | 5 uç |
| API durdurma | 5198 boş | **0 dinleyici** | `lsof -nP -iTCP:5198` |
| 5199 | dokunulmadı | **hâlâ ayakta** | `lsof` 2 satır |

**Test kiracısı AÇILMADI** (şema/veri değişikliği yok, gerek yoktu).
`tenants` sayısı: **başlangıç 9 → bitiş 9.** `QA-TEST-*` fikstürlerine
dokunulmadı (yalnız okundu).

### 8.3 46 rota × 375 / 768 / 1440 px ölçümü (DoD c) — KOŞULDU

Gerçek Chrome (headless, CDP), örnekleme yok, `main.ana-icerik.scrollWidth`:

| Genişlik | Rota | Taşan |
|---|--:|--:|
| 375 px | 46 | **0** |
| 768 px | 46 | **0** |
| 1440 px | 46 | **0** |

Ham veri: `scratchpad/olc36c2/olcum-3boy.json` (138 kayıt, `msw` boş kayıt 0).
Betik: `scratchpad/olc36c2/olc3.js`.
⚠ Dürüstlük notu: 46 rotanın 45'i ayrı bir yola çözüldü; `/giris` oturum açık
olduğu için `/`a yönleniyor, yani **giriş ekranı bu turda ölçülmedi**.

---

## § 9. YAPILMAYANLAR (paket bu yüzden YARIM)

1. **HAM_INPUT → 0 (ham girdi göçü) HİÇ BAŞLANMADI.** Ölçüm 357, tavan 357.
   `Bilesenler/Form/` ailesi (12 üye) hazır; serbest ekranlardaki ~330 ham
   `<input>/<textarea>` dönüştürülmedi. **Tek başına bir paket büyüklüğünde.**
2. **QA turu ve KABUL EŞİĞİ ölçülmedi** (görsel ≥ 4,0 · kullanıcı dostu ≥ 3,6).
   QA turu `betikler/qa-turu.js` ile **çok ajanlı workflow** olarak koşuyor;
   uygulayıcı ajanın elinde o koşum mekanizması yok. **Eşik ölçülmediği için
   "karşılanmadı" sayılmalıdır.** Sahte puan üretmedim.
3. **Kilitli 9 ekranın 26 ham `<select>`i** kapsam dışı; kendi paketlerine
   devredildi (Faturalar 10 · Siparisler 5 · Raporlar 4 · Sayim 3 · Pos 2 ·
   KasaRaporu 1 · KarZarar 1).
4. **"3 köşe yarıçapı → 1, 3 gölge → 2 kademe, 2 başlık puntosu → 1,
   5 düğme-satırı sınıfı → 1, 3 'eşit genişlik' yöntemi → 1"** iddiaları
   **kaynakta doğrulanamadı**: `.modal-panel`/`.cekmece` zaten tek yarıçap
   (`--radius-xl`) ve tek gölge (`--golge-xl`) belirteci kullanıyor. Bu sayılar
   ikinci frontend ajanının **tarayıcı ölçümünden** geliyor ve o rapor elimde
   yok. Uydurma sayaç açmadım (md.1); ölçemediğim iddiayı "yapıldı" da demedim.
5. **Dışarı tıklamada dolu formu onaysız kapatmama** ve **iki pencere aynı anda
   açılamaz** kuralları uygulanmadı.
6. **Kaydırma kilidi / odak dönüşü için gerçek tarayıcı testi** (doğrulayıcının
   DÜŞÜK bulgusu) eklenmedi; `C36c_7` hâlâ dizge sınıyor. 46 rota ölçümü
   koşuldu ama JS kilidinin davranışını sınamıyor.
7. **Sıfır baytlık 8 migration** (15 Eyl, `FiyatListesiVeKampanya`) duruyor —
   bu paketin borcu değil, ayrı temizlik kalemi.

---

## § 10. KURUCUYA / TEMİZLİK NOTLARI

* **Silinmesi gereken artıklar** (TurkReport hook `rm`i blokluyor, kurucuya
  bırakıldı):
  * `dotnet/Kuyumcu.Tests/GeciciTaniTestleri.cs` — **bu turda benim açtığım**
    geçici tanı dosyası; içeriği `printf '' >` ile boşaltıldı, 0 bayt duruyor.
  * Önceki turdan kalan 5 `*.mutorj` ve 3 `*.bak` dosyası.
* **Güvenlik kusuru — kendi hatam, gizlemiyorum:** MySQL parolasını değişkene
  alırken `grep … | head -1` yorum satırını yakaladı; kusuru bulmak için
  `docker-compose.yml`in ilgili satırını ekrana bastım ve **parola bu oturumun
  çıktısına düştü**. Doğru arama (`grep -m1 'MYSQL_ROOT_PASSWORD:'`) sonrasında
  bir daha basılmadı. Parola yerel geliştirme kabına aittir ve depoda zaten düz
  metindir, ama **oturum dökümü paylaşılacaksa bu değer değiştirilmelidir.**
* Mali müşavire / mevzuata giden bir hüküm bu pakette **yok** (yalnız arayüz).

---

## § 11. DoD SÖZLEŞMESİ

Bu paket **yeni varlık/kayıt türü DOĞURMADI** — ne tablo, ne uç, ne DTO alanı.
Dolayısıyla PATCH/DELETE/yetki/denetim kaydı yükümlülüğü **doğmadı**.
Mevcut kayıtların düzenlenebilirliği **değişmedi**; değişen tek şey, düzenleme
formlarındaki seçicilerin **eleman türü** ve kilitli seçicilerin artık
**sebebini söylemesi** (UX-S25/UX-S30 lehine iyileşme).

| Varlık | Düzenleme yolu | Değişti mi |
|---|---|---|
| (yeni varlık yok) | — | — |

## § 12. KOŞUL KARŞILAMA

| Koşul | Durum | Kanıt |
|---|---|---|
| HAM_SELECT → 0 (serbest) | **KARŞILANDI** | `HAM_SELECT_SERBEST = 0`, `C36c2_1`, R1 |
| HAM_INPUT → 0 (serbest) | **KARŞILANMADI** | 357, hiç başlanmadı |
| PENCERE_GENISLIK_CESIDI ≤ 3 | **KARŞILANDI** | 2, tavan 3, R4 |
| PENCERE_DUGME_SIRASI_TERS = 0 | **KARŞILANDI** | 0, R2 |
| ODAK_TUZAGI = 0 | KARŞILANDI (önceki tur) | denetim.sh |
| BOS_DURUM_METINSIZ = 0 | **KARŞILANDI** | 0, R3 |
| SABIT_RENK tavanı düşecek | **KARŞILANMADI** | ölçülmüş azalma yok, uydurulmadı |
| Kabul eşiği (QA görsel ≥4,0 · dostu ≥3,6) | **ÖLÇÜLMEDİ → karşılanmadı** | § 9/2 |
| (a) göç edilen ekranların testleri yeşil | KARŞILANDI | 3624/3624 |
| (b) mutasyon kanıtı | KARŞILANDI | 7/7 kırmızı, § 7 |
| (c) 46 rota × 3 çözünürlük | **KARŞILANDI** | taşma 0, § 8.3 |
| (d) denetim.sh TEMİZ | KARŞILANDI | § 8.1 |
| (e) tam süit 0 başarısız | KARŞILANDI | trx `failed="0"` |
| (f) test kiracısı silindi | KARŞILANDI (açılmadı) | tenants 9 → 9 |
| (g) rapor + kimlik tablosu + önce/sonra | KARŞILANDI | bu belge |

## § 13. KİMLİK KAPANIŞ TABLOSU

Göç edilen 58 seçicinin **kimliği birebir korundu**; yalnız kimliği OLMAYAN
6 süzgeç seçicisine yeni kimlik verildi (`<önek>-<alan>-filtre` kuralı):

| Yeni kimlik | Ekran | Eski hâli |
|---|---|---|
| `urun-ayar-filtre` | Urunler | kimliksiz, `aria-label="Ayar"` |
| `urun-kategori-filtre` | Urunler | kimliksiz, `aria-label="Kategori"` |
| `urun-reyon-filtre` | Urunler | kimliksiz, `aria-label="Reyon"` |
| `uy-supheli-durum` | Uyum | kimliksiz süzgeç |
| `cari-tur` … `ekstre-cari-{id}` | (21 ekran) | **kimlik AYNEN korundu** |

Mutasyon **R6** bu kilidi kanıtlıyor: tek bir kimliği değiştirmek 2 mevcut
ekran testini kırmızıya döndürüyor.

## § 14. ÖNCE / SONRA

| | ÖNCE | SONRA |
|---|--:|--:|
| Serbest ekranda ham `<select>` | 58 | **0** |
| `<Secici>` kullanımı | 97 | **155** |
| Kilit sebebi yazmayan kilitli seçici | 12 | **0** |
| Ters sıralı pencere düğmesi | 5 (+Çekmece kalıbı) | **0** |
| Pencere bileşenlerinde çıplak `catch` | 5 | **0** |
| denetim.sh P-36c sayacı | 4 | **8** |
| Derleme uyarısı | 1 | **0** |
| Test | 3617 | **3624** |
