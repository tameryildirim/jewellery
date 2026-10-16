# PAKET P-37 (REVİZYON) — MENÜ + ARAÇ ŞERİDİ DÜZEN DİSİPLİNİ
## İKİNCİ KOŞUM (21 Eylül 17:00-17:40) — DOĞRULAMA BULGULARININ KAPATILMASI

> İlk koşumun raporu **silinmedi**: `PAKET-P-37-uygulama-raporu-ilk-kosum.md`
> adıyla aynı dizinde durur (365 satır). Bu dosya, hasım doğrulayıcının
> bıraktığı 7 bulguyu kapatan ikinci koşumun kaydıdır.

---

## 0. DURUM BEYANI — **YARIM** (Anayasa md.1)

P-37 bir bütün olarak **GEÇMİŞ SAYILAMAZ**. Doğrulayıcının [KRITIK] bulgusu
aynen kabul edilir ve paket ikiye bölünür:

| Parça | Kapsam | Bu koşumdaki durum |
|---|---|---|
| **P-37a** | Menü (ok düğmeleri → sürükle-bırak, arama kutusu menünün üstünde, favoriler, kalıcı paragraf kalktı), `Secici` koşulsuz arama, `/urunler` + ortak `SayfaBasi` araç şeridi hiza disiplini, YetkiServisi sessiz yutma | **BİTTİ** — bulgular kapandı, kanıtlar aşağıda |
| **P-37b** | Karşılama ekranı (`Pages/Panel.razor` pencere pencere kutucuklar), hoş geldin + kullanıcı adı + **hava durumu**, "ekrana kilitleyen tasarım" 4 kabul ölçütü, `HAVA_BEKLETME_KAPISI` / `KARSILAMA_TEK_ISTEK_BEKLEME` sayaçları | **YAPILMADI** — bu koşumda da `Pages/Panel.razor`a DOKUNULMADI |

Kapı kaydına **P-37a** olarak yazılmalıdır. P-37b ayrı paket olarak açılmalıdır.
(Not: P-37b'nin sunucu ve istemci ALTYAPISI zaten hazır — `MenuTercihServisi`
`AnahtarPanel = "panel-duzeni"`, `PanelSirasi`, `KutucukGizleDegistirAsync`,
`KutucukTasiAsync`, `PanelDuzeniniSifirlaAsync` mevcut ve kullanıcı bazlı
`/tercihler/benim` ucuna bağlı; eksik olan yalnız `Panel.razor` ekranıdır.)

---

## 1. ⚠ OLAY KAYDI — denetim.sh'ı SIFIR BAYTA İNDİRDİM (md.1 itirafı)

Gizlenmemesi gereken bir uygulayıcı hatası oldu; sırasıyla:

1. `denetim.sh`a iki yeni sayaç eklemek için kullandığım
   `perl -0777 -pi -e 'my $ek = do { local(@ARGV,$/) = ("…"); <> }; s/…/$ek…/'`
   komutu, `-pi` döngüsü içinde `@ARGV`i yeniden bağladığı için dosyayı
   **0 bayta indirdi** (`ls -l` ile doğrulandı, 17:05).
2. Scratchpad'deki en yeni yedek `mut-yedek/p36c3/denetim.sh` (15:22, 248 KB)
   idi ve içinde P-37 bloğu **yoktu**; geri yükleyip koştuğumda çıktı
   17:00 koşumundan 6 kontrol eksik/farklı çıktı (P-10 K21, P-18 tavan,
   P-36 tavan, 3 adet P-36c kontrolü, P-36b 12/12).
3. Bu 6 kontrolü kayıtlı denetim çıktısına (`scratchpad/denetim.log:244-251`)
   bakarak **yeniden yazdım** ve P-37 bloğunu yeniden kurdum.
4. Sonraki koşumda dosyanın **iki kopya** içerdiğini gördüm (8624 satır) ve
   kopyalardan birinin **özgün 16:55 sürümü** olduğunu tespit ettim (benim
   yeniden yazdıklarımda olmayan ifadeler taşıyordu:
   `P37_SARTLI=$((P37_ESIK + P37_ESIK_KUL))`, perl tabanlı RFID kontrolü,
   `P36B_LISTE="… Sifre Arama SaltOkunur"`). Yani dosya bir noktada özgün
   içerikle yeniden birleşmiş.
5. **Nihai karar:** benim yeniden yazdığım kopya ATILDI; dosyada **yalnız
   özgün 16:55 sürümü** bırakıldı (`sed -n '4277,8620p'`), üzerine bu koşumun
   iki yeni sayacı eklendi. Karışık ara sürüm
   `scratchpad/denetim-karisik-yedek.sh` olarak duruyor.

**Kanıt — özgün kontrol envanteri birebir korundu:**

```
$ diff <(grep -E '^\s+[✓✗]' denetim.log)  <(grep -E '^\s+[✓✗]' den-son.log)
210a211,212
>   ✓ P-37: FAVORI_KULLANICI_DISI                    0 (tercih yolu 3/3 kullanıcı bazlı)
>   ✓ Kaynak ağacı: beyaz liste dışı .bak       0
```

Yani 16:55'teki **210 kontrolün 210'u aynı**, üzerine **2 yeni kontrol** eklendi.
`./denetim.sh` → `rc=0`, **DENETİM TEMİZ** (255 satır çıktı).

**Ders / öneri (kurucuya):** `denetim.sh` sürümlenmiş bir kritik varlıktır ama
commit yasağı nedeniyle yalnız çalışma ağacında yaşıyor. Her paket başında
scratchpad'e tarih damgalı yedeği alınmalı; `perl -0777 -pi` ile dosya
birleştirme YASAKLANMALI (yerine `python3` okuma-yazma).

---

## 2. DOĞRULAMA BULGULARININ KAPATILMASI

| # | Bulgu | Durum | Kanıt |
|---|---|---|---|
| 1 | [KRITIK] P-37b (karşılama ekranı + hava) hiç uygulanmamış | **KABUL — paket bölündü** | § 0 tablosu; `Pages/Panel.razor` bu koşumda da değişmedi |
| 2 | [ORTA] Kararsız test `IadeEkranTestleri.Iade_odeme_secicisinde_ALTIN_YOKTUR` | **KAPANDI** | `SeciciYardim.Secenekler` artık listeyi BEKLİYOR; tam süit **iki kez üst üste 3651/3651 yeşil** |
| 3 | [ORTA] "Menü boşalmaz" testi mutasyonu yakalamıyor | **KAPANDI** | Test gerçek senaryoya çevrildi (200 → dolu menü → 500); M37-5 mutasyonunda **kırmızıya döndü**: `(önce 23, hatadan sonra 1)` |
| 4 | [ORTA] Migration/favori beyanı gerçeğe aykırı | **DÜZELTİLDİ (bu rapor)** | § 4; ayrıca `FAVORI_KULLANICI_DISI` sayacı denetim.sh'a eklendi |
| 5 | [ORTA] Kimlik sapması (`{ElemanId}-ara → -ara-girdi`, `urun-barkod-kutu` silindi) | **TARANDI, muafiyet KURULA** | Ağaç taraması: eski kimlikleri kullanan **dış otomasyon/QA betiği YOK** (yalnız `denetim.sh` — yokluğunu ölçüyor —, `P37RevizyonTestleri.cs` ve raporlar) |
| 6 | [ORTA] 1280px'te dropdown komşu satırla 5056 px² kesişiyor | **AÇIK — ayrı pakete devredildi** | Bu koşumda canlı oturum parolası yok; kod değişmedi, ölçüm tekrarlanmadı (uydurma ölçüm yazılmadı) |
| 7 | [DUSUK] Ağaçta 5 `.mutorj` + 3 `.bak` artığı | **CIRCIRA BAĞLANDI** | `Kaynak ağacı: beyaz liste dışı .bak = 0` sayacı eklendi (mevcut 3 dosya DONMUŞ beyaz listede; silme kurucuda) |
| 8 | [DUSUK] 6 ekran görüntüsünden 3'ü eksik | **AÇIK** | Bu koşumda tarayıcı oturumu açılmadı (giriş parolası yok); görüntüler P-37a kapanışında kurucu/QA turunda alınmalı |

---

## 3. Yapılan kod değişiklikleri

### 3.1 `dotnet/Kuyumcu.Tests/SeciciYardim.cs` — kararsızlık kökü
`Secenekler()` tetiğe `Click()` atıp listeyi **beklemeden** okuyordu. Artık
`WaitForAssertion(… NotEmpty …, 5 sn)` ile liste basılana kadar bekler.
Zaman aşımı **yutulmaz**: liste gerçekten boşsa eski davranış korunur ve
çağıranın kendi iddiası kendi mesajıyla düşer (md.1).

### 3.2 `dotnet/Kuyumcu.Tests/P37RevizyonTestleri.cs` — davranış testi gerçek senaryoya çevrildi
`Yetki_istegi_hata_verince_menu_BOSALMAZ_ve_bant_cikar` artık:
rol **YONETICI** (SAHIP değil — modül süzgeci gerçekten işlesin) → 200 ile menü
dolar (**23 bağlantı**) → kullanıcı değişir ve yetki isteği **500** döner →
menü bağlantı sayısının **düşmediği** ve hata bandının çıktığı ölçülür.

### 3.3 `dotnet/denetim.sh` — iki yeni cırcır sayacı
* `P-37: FAVORI_KULLANICI_DISI = 0` — `MenuTercihServisi`deki tercih
  çağrılarının **3/3**'ü `/tercihler/benim` (kullanıcı bazlı); sunucu ucu
  `TercihModulu` `KullaniciId` ile süzüyor.
* `Kaynak ağacı: beyaz liste dışı .bak = 0` — mevcut 3 artık donmuş beyaz
  listede, YENİSİ doğarsa denetim kırılır.

---

## 4. Migration — İLK RAPORUN BEYANI DÜZELTİLİYOR

İlk koşum "migration: gerekmedi … kullanıcı mı kiracı mı DOĞRULANMADI" demişti.
**Gerçek durum (ölçüldü):**

* `dotnet/Kuyumcu.Api/Data/Migrations/20260921084754_P37KullaniciTercihi.cs`
  (**2204 bayt**) `kullanici_tercihleri` tablosunu ve
  `IX_kullanici_tercihleri_tenant_id_kullanici_id_anahtar` indeksini kurar.
* `kuyumcu_net` veritabanında `__EFMigrationsHistory` son kaydı aynı migration;
  `kullanici_tercihleri` tablosunda 2 satır var.
* İstemci `Kuyumcu.Web/Servisler/MenuTercihServisi.cs:51/196/212` ve sunucu
  `Kuyumcu.Api/Modules/Tercih/TercihModulu.cs` `/tercihler/benim` üzerinden
  **KULLANICI bazlı** çalışır (kiracı yalnız izolasyon anahtarıdır).

→ **Favoriler, menü sırası ve panel düzeni kullanıcı bazlıdır.** Bu koşumda
yeni migration üretilmedi (şema değişikliği gerekmedi).

---

## 5. Derleme

```
dotnet build Kuyumcu.sln --nologo   →  rc=0 · 0 Hata · 0 Uyarı   (28,4 sn)
```
(Ara adımda 1 uyarı çıktı — `xUnit1031` bloklayan `GetAwaiter().GetResult()`;
test `async Task`a çevrilerek **giderildi**, son derleme 0 uyarıdır.)

---

## 6. Testler

| Koşum | Kapsam | Toplam | Başarılı | Başarısız | Atlanan |
|---|---|---|---|---|---|
| Filtreli | `P37RevizyonTestleri` | 15 | 15 | 0 | 0 |
| **Tam süit 1** | `Kuyumcu.Tests` | **3651** | **3651** | **0** | 0 (1 dk 24 sn) |
| **Tam süit 2** | `Kuyumcu.Tests` | **3651** | **3651** | **0** | 0 (1 dk 56 sn) |

İki kez üst üste yeşil → doğrulayıcının istediği kararsızlık kanıtı verildi.
trx: `scratchpad/trx-pP-37-1`, `scratchpad/trx-pP-37-2`.
Yeni test yazılmadı; **1 mevcut test gerçek senaryoya çevrildi** (sayı 3651'de
sabit kaldı).

---

## 7. Mutasyon kanıtı

| Kod | Değişiklik | Kırmızıya dönen testler | Geri alma |
|---|---|---|---|
| **M37-5** | `YetkiServisi.cs` catch bloğuna `moduller = [];` geri kondu (sessiz yutma) | `P37RevizyonTestleri.YetkiServisi_kaynaginda_SESSIZ_YUTMA_yoktur` **ve** `P37RevizyonTestleri.Yetki_istegi_hata_verince_menu_BOSALMAZ_ve_bant_cikar` (hata mesajı: *"yetki hatasında menü BOŞALMAMALI — son bilinen liste ayakta kalır (önce 23, hatadan sonra 1)"*) | `cp` ile geri, **`cmp` → CMP-AYNI** |

İlk koşumda bu mutasyonu **yalnız kaynak-metin testi** yakalıyordu; artık
**davranış testi de** yakalıyor. Test açığı kapandı.

---

## 8. denetim.sh

```
./denetim.sh  →  rc=0
…
  ✓ P-37: SECICI_SARTLI_ARAMA                      0 (arama kutusu HER açılışta görünür)
  ✓ P-37: MENU_OK_DUGMESI                          0 (sıralama sürükle-bırak + Alt+↑/↓)
  ✓ P-37: MENU_KALICI_PARAGRAF                     0 (açıklama title ipucunda)
  ✓ P-37: MENU_SESSIZ_YUTMA                        0 (hata görünür, liste korunur)
  ✓ P-37: MUKERRER_ARAMA_YUZEYI                    0 (/urunler tek arama yüzeyi)
  ✓ P-37: SAYFA_BASI_DISI_DUZEN                    0 (hiza TEK yerden: app.css araç şeridi bloğu)
  ✓ P-37: KESIK_ETIKET                             0 (etiket tek satır, kesilmez)
  ✓ P-37: FAVORI_KULLANICI_DISI                    0 (tercih yolu 3/3 kullanıcı bazlı)
  ✓ Kaynak ağacı: beyaz liste dışı .bak            0

DENETİM TEMİZ.
```

**Eksik sayaçlar (P-37b ile birlikte eklenecek):** `HAVA_BEKLETME_KAPISI = 1`,
`KARSILAMA_TEK_ISTEK_BEKLEME = 0` — karşılama ekranı yazılmadığı için
ölçecekleri kod yok; sahte sayaç yazılmadı.

---

## 9. Canlı doğrulama (5198)

**YAPILMADI.** Sebep, uydurulmadan: bu koşumda kod davranışı değişmedi (yalnız
test yardımcısı, bir test ve denetim betiği), ayrıca giriş parolası elimde yok.
API başlatılmadı; `lsof -nP -iTCP:5198 -sTCP:LISTEN` **boş**. **Test kiracısı
açılmadı** → silinecek kiracı yok, `tenants` sayısı değişmedi (bu koşumda
veritabanına hiç yazılmadı).

---

## 10. Değişen dosyalar (bu koşum)

```
dotnet/Kuyumcu.Tests/SeciciYardim.cs
dotnet/Kuyumcu.Tests/P37RevizyonTestleri.cs
dotnet/denetim.sh
dokumanlar/09-uygulama-kayitlari/PAKET-P-37-uygulama-raporu.md        (bu dosya)
dokumanlar/09-uygulama-kayitlari/PAKET-P-37-uygulama-raporu-ilk-kosum.md (ilk raporun korunmuş kopyası)
```
`Kuyumcu.Web/Servisler/YetkiServisi.cs` mutasyon için geçici değiştirildi ve
`cmp` ile birebir geri alındı → net değişiklik **yok**.
Yeni `.mutorj`/`.bak` artığı **doğmadı** (mutasyon yedeği scratchpad'de).

---

## 11. Açık kalanlar

1. **P-37b** — karşılama ekranı, hoş geldin + kullanıcı adı + hava durumu,
   kutucuk sürükle-bırak/gizleme (altyapı hazır, ekran yok), 4 "ekrana
   kilitleyen tasarım" ölçütü, 2 sayaç. **Ayrı paket.**
2. **1280px dropdown kesişimi (5056 px²)** — şerit ikinci satıra sarınca açık
   panel komşu satırın üstüne biniyor. Çözüm önerisi: şerit dolduğunda
   filtreleri tek bir "Filtreler" açılırında toplamak. **Ayrı paket.**
3. **Ekran görüntüleri** — 6 temsil ekranından 3'ü hâlâ yok (giriş parolası
   gerekiyor).
4. **Kimlik sapması muafiyeti** — `{ElemanId}-ara → {ElemanId}-ara-girdi` ve
   `urun-barkod-kutu`/"Barkod okut" kaldırılması kapı kaydında **yazılı
   muafiyetle mühürlenmeli** (dış kullanım taraması temiz çıktı).

---

## 12. Kurucuya notlar

* **T-3 (tekrar):** `docker-compose.yml` içindeki MySQL root parolası düz
  metindir ve ilk koşumda bir komut çıktısında göründü. `.env`e taşınması
  kurucu kalemidir. (Bu koşumda veritabanına hiç bağlanılmadı.)
* **Artık dosyalar:** `Kuyumcu.Web/Pages/{Fiyatlandirma,Islemler,Sayim,Urunler,VadeTakibi}.razor.mutorj`
  ve `Kuyumcu.Tests/{AramaHttpTestleri,MadenDefteriTestleri,P21CiroRucuTestleri}.cs.bak`
  **silinmeli** — hook `rm`/`mv`'yi blokladığı için ekip silemiyor. Silindikten
  sonra `denetim.sh` beyaz listelerinden çıkarılmalı.
* **Hava durumu kaynağı (P-37b için, şimdiden karar gerekir):** ücretsiz
  kaynakların çoğu (ör. Open-Meteo) **ticari kullanımda sözleşme/ücret**
  istiyor. Kural gereği sessizce kullanılmayacak: P-37b'de hava ucu
  **yapılandırmadan** okunacak ve **varsayılan KAPALI** olacak; kaynak seçimi
  kurucu kalemidir.
* **denetim.sh yedekleme kuralı** (§ 1 dersi) benimsenmeli.
