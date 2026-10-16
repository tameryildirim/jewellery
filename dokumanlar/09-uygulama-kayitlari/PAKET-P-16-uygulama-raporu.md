# PAKET P-16 — SATIŞ KALEMİ + FORM ALANI PARİTESİ (Islemler.razor TEK EL)

**Tarih:** 2026-09-21 · **Kimlikler:** Y-4 · Y-26 · Y-27 · B-31 · **+ P-00 triyajı:** B-69
**Dalga:** DALGA 4 · **Sürüm:** 1.0 · **Rol:** Taşkın (03 Backend) + Ertuğ (03 Frontend) · **Model:** Opus
**Durum:** **BİTTİ**
**COMMIT YOK** · git komutu çalıştırılmadı (yalnız `git status --porcelain` okundu) · **5199'a DOKUNULMADI**
(kurucunun inceleme sunucusu; canlı doğrulama 5198'de yapıldı ve süreç durduruldu).

---

## § 0 — ÖNKOŞUL ÖLÇÜMÜ (P-15)

| Önkoşul | Diskte | Ölçüm |
|---|---|---|
| **P-15** | **VAR** — `dokumanlar/09-uygulama-kayitlari/PAKET-P-15-uygulama-raporu.md` | Kabuk bileşenleri (`KayitSecici`, `GlobalArama`, `SubeSecici`, `DerinBaglanti`) diskte; `Islemler.razor`'a **dokunmadığı** doğrulandı (P-15 raporu DoD'unda yazılı, `Pages/Islemler.razor` P-15 değişiklik listesinde yok). |

Kapsam daraltması **gerekmedi**. P-15'in açık bıraktığı `DUZ_SELECT_LIMIT` kalemi
(`Islemler.razor`'daki 2 nokta) bu pakette **kapatılmadı** — bkz. § 9 açık kalanlar; P-16'nın
kimlikleri arasında değil ve kapsam kilidi gereği büyütülmedi.

### Taze taban ölçümleri (belgeden KOPYALANMADI)

| Ölçüm | Değer | Komut |
|---|---|---|
| `__EFMigrationsHistory` (başlangıç) | **55** | `docker exec … mysql -N kuyumcu_net -e "select count(*) from __EFMigrationsHistory"` |
| `tenants` (başlangıç) | **8** | aynı |
| `QA-TEST-%` kiracı | **3** | aynı (dokunulmadı) |
| `urunler` kolonları | tedarikci_id, alis_maliyeti, hedef_marj, kdv_orani, seri_no, olcu, min_stok → **7'si de YOK** | `information_schema.columns` |
| `cariler` kolonları | unvan, kimlik_turu, dogum_tarihi, uyruk, iskonto_yuzde, segment → **6'sı da YOK** | aynı |
| `Pages+Bilesenler+Layout` satır içi `style="` | **154** | `grep -rhoE 'style="' --include='*.razor' …` |
| Tam süit (P-16 öncesi) | **3268** | 3307 − 39 (P-16 testleri) |

---

## § 1 — SPESİFİKASYONUN KODA KARŞI DOĞRULANMASI (ADIM 1)

| PLAN'ın iddiası | Kodda gerçek | Karar |
|---|---|---|
| `Data/Entities.cs:108` — **`vergi_dairesi` ZATEN VAR**, migration listesinden çıkarıldı | **DOĞRU ama BAŞKA VARLIKTA.** `vergi_dairesi` `Entities.cs:115`'te ve **`Tenant`** sınıfındadır (işletme kimliği, UX-1 Faz B), `Cari`'de değildir. | PLAN'ın **hedefi** (mevcut kolonu migration'a yazma, T-5 ihlali) korundu: `vergi_dairesi` migration'a **yazılmadı**. `Cari`'ye de eklenmedi — PLAN onu listeden düşürmüştü, kapsam büyütülmedi. |
| `cariler`: **Notlar** mevcut, listeden düşülür | **DOĞRU** — `Cari.Notlar` var (P-10/Y-3). | Migration'a yazılmadı. |
| `cariler`: "**TCKN/VKN ayrımı**" | Bugün TEK kolon (`vergi_no`, MaxLength 20) ve üzerinde **kiracı içi benzersizlik kısıtı** (`UQ_cariler_tenant_vergi_no`, P-05/Y-2) var. | **SAPMA S-1** (§ 2). Ayrım ikinci NUMARA kolonuyla değil, **`kimlik_turu`** kolonuyla yapıldı. |
| `urunler`: 7 kolon | 7'si de yok — **DOĞRU** | Hepsi eklendi. |
| `Pages/Islemler.razor:434-441,1771-1772` | Satır numaraları **kaymış** (dosya 2306 satır; kalem formu 400-540, `ToplamTl` 1699 civarı). | Koda bakılarak çalışıldı; PLAN'ın satır numaraları değil **hedefi** esas alındı. |
| Y-26'nın kaynak satırı "marka/model" de sayıyor | PLAN'ın **migration listesi** marka/model içermiyor (7 kolon). | PLAN bağlayıcıdır: marka/model **açılmadı**. § 9'da "devredildi" olarak yazılı. |

**Mevzuat işareti:** PLAN P-16'da `DOGRULANMALI/VARSAYIM` damgalı bir hüküm **yok**. Yine de
`kdv_orani` kolonu için tutucu davranıldı: **oran koda SABİT YAZILMADI**, alan boş bırakılırsa
mevcut KDV motoru (özel matrah rejimi) karar verir; kolon yalnız **elle ezme** yoludur.
TCKN sağlama (checksum) algoritması **uygulanmadı** — PLAN DoD'u "11/10 hane" der (§ 9, mali müşavir notu).

---

## § 2 — SAPMALAR VE GEREKÇELERİ (Anayasa md.1)

### S-1 — TCKN/VKN ayrımı İKİNCİ NUMARA KOLONUYLA DEĞİL, TÜR KOLONUYLA yapıldı
PLAN "TCKN/VKN ayrımı" diyor, kolon adı vermiyor. İki yol vardı:
1. `tckn` diye ikinci numara kolonu açmak,
2. numarayı tek kolonda tutup **türünü** ayrı kolonda yazmak.

**(2) seçildi.** Gerekçe: `vergi_no` üzerinde **P-05 (Y-2) kiracı içi benzersizlik kısıtı** var ve
MASAK/fatura/gider pusulası yolları o kolonu okuyor. İkinci numara kolonu açılsaydı aynı kişi iki
kolonda iki kez benzersiz olur, kısıt fiilen delinir ve cari yine ikiye bölünmüş bakiyeyle kalırdı —
yani P-05'in kapattığı hata geri gelirdi. `kimlik_turu` (TCKN|VKN) kolonu ayrımı **veri göçü
riski olmadan** kurar; hane doğrulaması bu türe bakar. Tür beyan edilmemiş eski kayıtlarda tür
**numaranın uzunluğundan türetilir**, uydurulmaz.

### S-2 — Dosya ayak izi genişletildi (5 dosya), gerekçesiyle
PLAN'ın "Dosyalar" satırı yalnız üç razor + `Entities.cs` + `Migrations/` + `Kuyumcu.Tests/` sayıyor.
Ancak aynı bloğun **DoD'u** sunucu değişikliği zorunlu kılıyor:

| Dosya | Neden ayak izi dışına çıkıldı |
|---|---|
| `Modules/Satis/SatisModulu.cs` | DoD: "satır ve fiş iskontosu **sunucuda yeniden hesaplanır**". Hesap sunucudadır; DTO ve uç burada. §7 TEK EL zincirlerinde bu dosya **yok**. |
| `Modules/Satis/IskontoHesabi.cs` (**YENİ**) | Hesabın **tek yeri**. SatisModulu içine gömülseydi saf (DB'siz) mutasyon testi yazılamaz, kural iki yerde çoğalırdı. |
| `Common/IslemDogrulayici.cs` | B-31: "pasif cariler satış listesinde görünmez" ekran kuralıdır; **ekranın gizlemesine güvenmek fail-open** olurdu. Kapı K1 kapısının olduğu yere kondu. |
| `Modules/Cari/CariModulu.cs` · `Modules/Urun/UrunModulu.cs` | Y-26/Y-27 alanları DTO+servis+yanıt olmadan forma bağlanamaz; `denetim.sh` P-10 sayacı da bu çiftleri ölçüyor. |
| `Kuyumcu.Core/Domain/KimlikNo.cs` (**YENİ**) · `Kuyumcu.Core/I18n/Sozlukler.cs` | TCKN/VKN kuralının **tek kaynağı** (sunucu + ekran aynı kuralı okur) ve TR+EN sözlük (çıplak metin yasak). |
| `Kuyumcu.Contracts/Sayfalar/{Islemler,Urunler,Cariler}Sozlesmeleri.cs` | "Pages içinde yeni istemci record AÇMA" koşulu gereği yeni alanlar **mevcut** tiplere Contracts tarafında eklendi. |
| `Kuyumcu.Web/wwwroot/css/app.css` | Satır içi `style` yasak (denetim.sh cırcırı); iki sınıf eklendi. |

**PLAN ile bu koşullar çelişmedi**; PLAN bloğunun "Dosyalar" satırı ile "DoD" satırı kendi içinde
eksik kalmıştı, DoD üstün tutuldu (P-15'in S-2 kaydıyla aynı gerekçe).

### S-3 — İskonto girilen satış TEK KALEMLİ uca değil FİŞ ucuna gider
`POST /islemler/satis` kalem başına tek işlem yazar ve **iskonto tanımaz**. Ekran iskonto girilmişken
oraya gitseydi oran **sessizce yok sayılır**, müşteriye söz verilen indirim deftere hiç yazılmazdı.
Bu yüzden `Islemler.razor` `HediyeVar || IskontoVar` olduğunda `/islemler/satis-fisi` ucuna gider.
Mutasyon **M16-9** bu kuralı kanıtlar.

### S-4 — `ToplamTl` artık NET
Ekranın `ToplamTl` değeri iskonto düşülmüş net tutardır. Sebep: ödeme bacakları, tevsik önizlemesi ve
nakit kilidi hep bu değeri okur; brüt gönderilseydi bacak toplamı sunucunun hesapladığı fiş toplamını
tutmaz ve fiş **400** dönerdi.

### S-5 — DÜRÜSTLÜK KAYDI: MySQL parolası bir kez çıktıya düştü
Oturumun başında `sed -n '1,25p' docker-compose.yml | grep -n "MYSQL"` komutu, dosyanın parolayı
**düz metin** içeren satırını çıktıya bastı. Sebep: ilk denemede `grep MYSQL_ROOT_PASSWORD … | head -1`
dosyanın **3. satırındaki yorumu** yakalamıştı (parola değil), hata aranırken dosyanın başı okundu.
Parola depoda zaten düz metindir (`docker-compose.yml` bunu kendi başında yazıyor) ama **çıktıya
basılmaması kuralı ihlal edildi**. Sonraki tüm komutlarda desen `^\s+MYSQL_ROOT_PASSWORD:` olarak
düzeltildi ve parola yalnız `$P` değişkeninde taşındı. **Bu, P-15 raporundaki S-5 ile aynı ihlaldir —
iki paket üst üste tekrarlandığı için § 9'da kurucuya kalem olarak yazıldı (K-5 sır rotasyonu).**

---

## § 3 — YAPILAN İŞ

### 3.1 Veri modeli (`Data/Entities.cs`)
**`Cari` +6:** `unvan(300)` · `kimlik_turu(4)` · `dogum_tarihi(DateOnly?)` · `uyruk(2)` ·
`iskonto_yuzde decimal(5,2)` · `segment(20)` — hepsi **nullable**, mevcut kartlar aynen kalır.
**`Urun` +7:** `tedarikci_id(36)` · `alis_maliyeti decimal(15,2)` · `hedef_marj decimal(6,2)` ·
`kdv_orani decimal(5,2)` · `seri_no(60)` · `olcu(20)` · `min_stok decimal(12,3)` — hepsi nullable.

`KuyumcuDbContext.cs`'e **DOKUNULMADI** (§7 TEK EL zinciri P-04→P-05→P-08→P-34, P-16 o zincirde yok).
Yeni **varlık** açılmadığı için yeni indeks gerekmedi; `urunler`/`cariler` üzerinde `(tenant_id)`
öncü indeksi zaten var.

### 3.2 Alan kuralı — `Kuyumcu.Core/Domain/KimlikNo.cs` (YENİ)
`KimlikNo.Dogrula(no, tur)` TEK kuraldır: sunucu DTO'su, servis içi elle kontrol ve ekranın alan altı
mesajı **aynı** kaynağı okur. `CariSegmentleri` kapalı listedir. Yalnız **hane sayısı + rakam**
denetlenir; TCKN sağlaması **uygulanmadı** ve bu koda yorum olarak yazıldı (tahmin yok).

### 3.3 İskonto motoru — `Modules/Satis/IskontoHesabi.cs` (YENİ)
- İstemciden **yalnız YÜZDE** alınır; TL karşılığı sunucuda üretilir (`Tutar`), 2 hane AwayFromZero.
- İskonto brütü **aşamaz** (kelepçe) — satır negatife inmez.
- Fiş iskontosu satırlara **oransal dağıtılır** (`Dagit`); kuruş artığı **en büyük satıra** yazılır,
  böylece **Σ satır = fiş toplamı TAM eşit** kalır (ödeme bacağı, KDV matrahı, stok maliyeti hep
  satır tutarını okur).
- Aralık dışı yüzde **reddedilir**, sessizce kelepçelenmez.

### 3.4 Uç sözleşmesi (`Modules/Satis/SatisModulu.cs`)
`SatisFisKalemDto.SatirIskontoYuzde` · `SatisFisiDto.FisIskontoYuzde` (`[Range(0,100)]`).
`SatisFisi` akışı: satır brütü → satır iskontosu → fiş iskontosu (dağıtılmış) → `fisToplam`.
**UX-S21:** `fisIskontoYuzde` **gönderilmezse** carinin `iskonto_yuzde` oranı uygulanır ve yanıtta
`fisIskontoCaridenMi: true` döner. Açıkça `0` gönderilirse **uygulanmaz** — "0" ile "boş" ayrı
anlamlardır. Yanıt **UX-S27** için genişletildi: `satirIskontoToplam` · `fisIskontoTutari` ·
`fisIskontoYuzde` · `fisIskontoCaridenMi` · `iskontoToplam`.

### 3.5 B-31 — pasif cari (iki katman)
- **Sunucu (fail-closed):** `IslemDogrulayici.CariAktifDogrula` — `Satis` ve `SatisFisi` yollarında.
  `CariDogrula`'ya **eklenmedi**: pasif cariden tahsilat/ekstre/düzeltme yapılabilmelidir.
- **Ekran:** `Islemler.razor` → `/cari?limit=200&aktif=true`; seçicinin altında sebep notu
  (`#islem-cari-pasif-notu`, UX-S29/S30 — sessiz kırpma yok).

### 3.6 Ekran — `Pages/Islemler.razor` (**TEK EL, tek geçiş**)
- Kalem tablosuna **Ayar · Milyem · Gram · Has** kolonları: değerler **ürün kartından türetilir**,
  kasiyerden istenmez (UX-S21).
- **Satır iskontosu** `SatirIciDuzenle` ile (`#si-kalem-iskonto-{urunId}`) — **TEK DÜZENLEME KALIBI**
  korundu, ikinci `duzenlenenId` kalıbı açılmadı, yeni `ModalPencere` düzenleme kullanımı **0**.
- **Fiş iskontosu** kutusu (`#islem-fis-iskonto`) + canlı önizleme (`#islem-iskonto-onizleme`).
- **B-69 / UX-S21:** birim fiyat zaten sunucudan dolduruluyordu; eksik olan **kaynağın rozetle
  söylenmesiydi** → `#islem-fiyat-kaynak` ("fiyat listesinden geldi").
- **UX-S27:** POST yanıtı okunur, uygulanan iskonto **başarı bandına** yazılır (`IskontoBandiniOku`).
- Tür değişiminde ve kayıt sonrası iskonto **sıfırlanır** (bir sonraki fişe sızmaz).

### 3.7 Ekran — `Pages/Urunler.razor` ve `Pages/Cariler.razor`
**UX-S26 paritesi:** her iki sayfada da **oluşturma formu ile düzenleme çekmecesi aynı alan kümesini**
taşır ve seçenek listeleri **tek diziden** üretilir (`KimlikTurleri`, `Segmentler`, `cariler`).
Yeni id'ler `<önek>-<alan>` düzeninde (`u-*`/`d-*`, `cari-*`/`duz-*`); **mevcut id'ler değişmedi**.
TCKN/VKN doğrulaması **alan altında** (`#cari-vergino-ipucu`, `AlanHatasi`).

### 3.8 Sözlük (TR + EN)
`SozlukCari` +19, `SozlukUrun` +15, `SozlukIslem` +13 anahtar; hepsi TR ve EN karşılığıyla.
`required init` olduğu için **eksik anahtar derleme hatası** verir; çıplak metin yok.

---

## § 4 — MIGRATION

**Ad:** `20260921001728_P16UrunCariKartAlanlari`
**Prosedür:** `dotnet build Kuyumcu.sln` **YEŞİL** (0 hata / 0 uyarı) → `dotnet ef migrations add …`
(**`--no-build` KULLANILMADI**) → dosyalar okundu → `dotnet ef database update --connection …`
(parola değişkenden).

| Dosya | Boyut |
|---|---|
| `20260921001728_P16UrunCariKartAlanlari.cs` | **4.970 bayt** (0 bayt değil) |
| `20260921001728_P16UrunCariKartAlanlari.Designer.cs` | **198.417 bayt** |

`Up()` **13 `AddColumn`**, `Down()` **13 `DropColumn`** (toplam 26 çağrı) — `Down()` **var**.
Boş/çakışan migration üretilmedi; `vergi_dairesi` ve `notlar` **yazılmadı** (T-5).

**Uygulama kanıtı (`kuyumcu_net`):**

| Sorgu | Önce | Sonra |
|---|---|---|
| `select count(*) from __EFMigrationsHistory` | **55** | **56** |
| `urunler`'de 7 yeni kolon | 0 | **7** |
| `cariler`'de 6 yeni kolon | 0 | **6** |

---

## § 5 — TESTLER

| Koşum | Toplam | Başarılı | Başarısız | Atlanan |
|---|---|---|---|---|
| `--filter FullyQualifiedName~P16` | **39** | 39 | **0** | 0 |
| **TAM SÜİT** (`--logger trx`) | **3307** | **3307** | **0** | 0 |

TRX sayaçları: `total=3307 executed=3307 passed=3307 failed=0 error=0 timeout=0 aborted=0`
(dosya: `…/scratchpad/trx-pP-16/_Mac_2026-09-21_03_34_10.trx`). Kırmızı **yok**, kararsızlık yok.

**Yeni test sayısı: 39** (P-16 öncesi 3268 → sonra 3307), iki dosyada:
`Kuyumcu.Tests/P16SatisKalemiTestleri.cs` (506 satır, 30 test: saf iskonto · saf TCKN/VKN ·
DB satır/fiş iskontosu · pasif cari · cari kartı · ürün kartı · kiracı sızıntısı) ve
`Kuyumcu.Tests/P16EkranTestleri.cs` (286 satır, 9 bUnit testi).

**Sahte API notu:** bUnit sahte API'de `{}` varsayılanı bırakılmadı — bu pakette dokunulan her uca
(`/cari`, `/cari/{id}`, `/urunler`, `/urunler/{id}`, `/fiyat/teklif`, `/masak/esikler`,
`/tevsik/gunluk`, `/pos/cihazlar`, `/kur/guncel`) **gerçekçi** yanıt verildi.

---

## § 6 — MUTASYON KANITI

Yöntem: kaynak `…/scratchpad/mut-yedek/P16/` altına kopyalandı → `perl -0777 -pi -e` ile **tek
eşleşmeli** değişiklik → filtreli `dotnet test` → yedek geri kopyalandı → **`cmp` ile birebir aynı**
olduğu kanıtlandı. Son mutasyondan sonra **TEMİZ `dotnet build`** (0 uyarı / 0 hata).

| Kod | Değişiklik | Kırmızıya dönen test(ler) | Geri alındı (`cmp`) |
|---|---|---|---|
| **M16-1** | `IskontoHesabi.Dagit` — kuruş artığı yazılmıyor (`fark = 0m`) | `P16IskontoHesabiTestleri.Fis_iskontosunun_satir_paylari_TOPLAMI_TAM_tutar` | ✓ birebir aynı |
| **M16-2** | `IskontoHesabi.Tutar` — brüt kelepçesi kalktı (`return t;`) | *(ilk denemede HİÇBİR test kırılmadı → **TEST AÇIĞI**)* `Iskonto_brutu_ASAMAZ` testi 10,005 × %100 vakasıyla **güçlendirildi**, mutasyon tekrarlandı → `P16IskontoHesabiTestleri.Iskonto_brutu_ASAMAZ` **KIRMIZI** | ✓ birebir aynı |
| **M16-3** | `IslemDogrulayici.CariAktifDogrula` — pasif kapısı kapalı (`if (false)`) | `P16SatisFisiIskontoTestleri.Pasif_cariyle_SATIS_FISI_REDDEDILIR` · `…Pasif_cariyle_TEK_KALEMLI_SATIS_da_REDDEDILIR` | ✓ birebir aynı |
| **M16-4** | `SatisFisi` — carinin iskonto oranı uygulanmıyor (UX-S21 bağı kopar) | `P16SatisFisiIskontoTestleri.Fis_iskontosu_bossa_CARININ_orani_uygulanir` | ✓ birebir aynı |
| **M16-5** | `KimlikNo.Dogrula` — hane sayısı denetlenmiyor | `P16KimlikNoTestleri.TCKN_11_haneden_kisa_ya_da_uzunsa_REDDEDILIR` · `…VKN_10_hane_olmali` · `P16KartAlanlariTestleri.Cari_olustururken_TCKN_hane_kurali_SERVISTE_de_isler` · `P16EkranTestleri.Eksik_haneli_TCKN_alan_altinda_mesaj_verir_ve_istek_ATILMAZ` | ✓ birebir aynı |
| **M16-6** | `UrunServisi.Guncelle` — `SeriNoGonderildi` koşulu kalktı (gönderilmeyen alan SİLİNİR) | `P16KartAlanlariTestleri.Urun_kart_alanlari_YAZILIR_ve_PATCH_ile_DUZELTILIR` | ✓ birebir aynı |
| **M16-7** | `CariServisi.Guncelle` — `UnvanVerildi` koşulu kalktı | `P16KartAlanlariTestleri.Cari_PATCH_gonderilmeyen_alani_KORUR_null_geleni_SILER` | ✓ birebir aynı |
| **M16-8** | `Islemler.razor` — `/cari?limit=200` (aktif süzgeci düştü) | `P16EkranTestleri.Islemler_cari_listesini_YALNIZ_aktif_suzgeciyle_ceker` | ✓ birebir aynı |
| **M16-9** | `Islemler.razor` — iskonto varken tek kalemli uca gidilir (`IskontoVar` koşulu kalktı) | `P16EkranTestleri.Fis_iskontosu_YUZDE_olarak_FIS_ucuna_gonderilir` | ✓ birebir aynı |
| **M16-10** | `Islemler.razor` — has kolonu ürün kartından türetilmiyor (`Bicim.Gram(0m)`) | `P16EkranTestleri.Kalem_satiri_ayar_milyem_gram_ve_has_GOSTERIR` | ✓ birebir aynı |

> **M16-2 dürüstlük kaydı:** ilk hâlinde mutasyon hiçbir testi kırmadı; test %100 iskontoyu tam sayı
> brütle sınıyordu ve kelepçe hiç devreye girmiyordu. Bu bir **test açığıdır**, testi güçlendirip
> mutasyon tekrarlandı ve kırmızı **adıyla** kanıtlandı.

Son durum: `cmp` tüm 7 kaynak dosyada **fark yok**; kaynakta `M16-*` damgası **kalmadı**.

---

## § 7 — DENETİM (`dotnet/denetim.sh`)

```
✓ P-16: DTO_FORM_ESLESMEZ                        0 (-eq 0, +2 çift · toplam 7 çift)
✓ P-16: BU_PAKETTE_EKLENEN_RECORD                0 (Islemler+Urunler+Cariler)
✓ P-16: PASIF_CARI_SATISTA                       ekran 2 · sunucu kapısı 2
✓ P-16: ISKONTO_TUTARI_ISTEMCIDEN                0 (yalnız yüzde taşınır, tutar sunucuda hesaplanır)

DENETİM TEMİZ.
```

### Mekanizma kuralı — eklenen/güncellenen cırcır sayaçlar (`# >>> P-16` … `# <<< P-16`)

| Sayaç | Değer | Ölçüm komutu betikte yorum olarak yazılı |
|---|---|---|
| `DTO_FORM_ESLESMEZ` | **0** · kapsam **5 → 7 çift** (PLAN: "kapsam genişler") | P-10'un `P10_KONTROL` fonksiyonu **yeniden kullanıldı** (ikinci kalıp açılmadı); eklenen çiftler: `SatisFisKalemDto`→`Islemler.razor`, `SatisFisiDto`→`Islemler.razor`. Kapsam `<7` olursa **kırmızı** (daraltılamaz). |
| `BU_PAKETTE_EKLENEN_RECORD` | **0** | `grep -c 'record …' Islemler/Urunler/Cariler.razor` → 0. Yeni alanlar `Kuyumcu.Contracts`'taki **mevcut** tiplere eklendi. |
| `PASIF_CARI_SATISTA` (yeni) | ekran 2 · sunucu 2 | Ekranın gizlemesi **tek başına yetmez**; sunucu kapısı da aranır. |
| `ISKONTO_TUTARI_ISTEMCIDEN` (yeni) | **0** | `SatirIskontoTl|FisIskontoTl` sözleşmede **olamaz** — bir gün eklenirse denetim kırılır. |
| `SATIR_ICI_STYLE` (mevcut, **tavan İNDİRİLDİ**) | **154** (tavan 155 → **154**) | Taze ölçüm depoda 154 gösterdi; cırcır yalnız azalır, tavan gerçeğe indirildi. P-16 hiç satır içi style eklemedi (`.satir--alt-hizali`, `.alan--dar` sınıfları). |

**Tavan/eşik hiçbir yerde ARTIRILMADI.**

---

## § 8 — CANLI DOĞRULAMA (localhost:**5198** — 5199'a DOKUNULMADI)

API: `ASPNETCORE_ENVIRONMENT=Development dotnet run --project Kuyumcu.Api --no-build
--no-launch-profile --urls http://localhost:5198` (**PID 14689**, iş sonunda `kill`).
Kimlik: test kiracısı SQL ile kuruldu; jeton yapılandırmadaki imza anahtarıyla **yerel üretildi**
(anahtar değişkende tutuldu, **hiçbir yerde basılmadı**). Yanıt gövdeleri **okundu** (`-o /dev/null`
yalnız 401 kod kontrolünde ve wasm indirmede kullanıldı).

| # | Adım | Beklenen | Gerçek |
|---|---|---|---|
| 1 | `GET /cari?limit=200&aktif=true` | yalnız aktif cari | **200**, `['P16 Aktif Musteri']` (pasif kart **yok**) |
| 2 | `POST /islemler/satis-fisi` **pasif cari** ile | 400 | **400** `"Pasif cariyle yeni satış yapılamaz. Önce cari kartını aktifleştirin…"` |
| 3 | 2 kalem × 1.000, satır %10 + fiş %10 | 1.620 TL | **200** `toplamTl=1620 · satirIskontoToplam=200 · fisIskontoTutari=180 · iskontoToplam=380` |
| 4 | `fisIskontoYuzde=150` | 400 | **400** `"Fiş iskontosu 0 ile 100 arasında olmalıdır"` (alan altı: `alanlar.fisIskontoYuzde`) |
| 5 | Cariye %25 yazıldı, gövdede iskonto **YOK** (UX-S21) | sunucu carinin oranını uygular | **200** `toplamTl=750 · fisIskontoTutari=250 · fisIskontoYuzde=25 · fisIskontoCaridenMi=true` |
| 6 | `POST /cari` TCKN + 10 hane | 400 | **400** `"T.C. Kimlik No 11 hane olmalıdır"` |
| 7 | `POST /cari` geçerli TCKN + 5 KYC alanı | 201, alanlar geri döner | **201** `unvan · kimlikTuru=TCKN · dogumTarihi=1985-04-12 · uyruk=TR · iskontoYuzde=7.50 · segment=VIP` |
| 8 | `PATCH /cari/{id}` yalnız `segment` | ünvan/iskonto **korunur** | **200** `unvan='P16 Kuyumculuk Ltd.' · iskontoYuzde=7.5 · segment=TOPTAN` |
| 9 | `PATCH /cari/{id}` `unvan:null` | ünvan **silinir**, diğerleri durur | **200** `unvan=None · segment=TOPTAN · iskontoYuzde=7.5` |
| 10 | `POST /urunler` 7 kart alanı | 201, hepsi yazılır | **201** `tedarikciId · alisMaliyeti=12500.5 · hedefMarj=22.5 · kdvOrani=20 · seriNo=SN-001 · olcu=17 · minStok=3` |
| 11 | `PATCH /urunler/{id}` yalnız `alisMaliyeti` | `seriNo`/`olcu` **korunur** | **200** `alisMaliyeti=9000 · seriNo=SN-001 · olcu=17` |
| 12 | `PATCH /urunler/{id}` `seriNo:null` | **silinir** | **200** `seriNo=None · olcu=17 · alisMaliyeti=9000` |
| 13 | `GET /urunler?limit=5` | milyem+gram+has döner (Y-4 ekran girdisi) | **200** `K22 · milyem 916.0 · gram 5.0/10.0 · has 4.58/9.16` |
| 14 | `POST /urunler` **başka kiracının** carisi tedarikçi | 404 | **404** `"Cari bulunamadı"` (K1 kapısı) |
| 15 | `POST /islemler/satis-fisi` **jetonsuz** | 401 | **401** |
| 16 | `_framework/Kuyumcu.Web.wasm` dizge taraması (UTF-16) | yeni id'ler var | `islem-fis-iskonto` **2** · `islem-cari-pasif-notu` **1** · `islem-fiyat-kaynak` **1** · `u-alis-maliyeti` **3** · `d-min-stok` **2** · `cari-kimlik-turu` **1** · `duz-segment` **1** |
| 17 | API durduruldu | `lsof -nP -iTCP:5198 -sTCP:LISTEN` boş | **0 satır** |
| 18 | 5199 | dokunulmadı | hiç istek gönderilmedi |

> **Ölçüm dürüstlüğü (16):** UTF-8 dizge taraması **hiçbir** element kimliğini bulmadı — **mevcut**
> `islem-kalem-ekle` dahi 0 çıktı. Sebep: Blazor kimlikleri wasm'da **UTF-16** saklıyor. Tarama
> UTF-16'ya çevrilince hepsi bulundu; ilk sonuç "ekranda yok" diye **yorumlanmadı**, kök sebep
> bulundu (Anayasa md.1).

### SQL kanıtı (`docker exec kuyumcu-mysql mysql … kuyumcu_net`, parola değişkenden)

| Sorgu | Başlangıç | Test sırasında | Son |
|---|---|---|---|
| `SELECT COUNT(*) FROM tenants` | **8** | 9 | **8** |
| `… WHERE ad LIKE 'PAKET%'` | 0 | 1 (`PAKET P-16-TEST`) | **0** |
| `… WHERE ad LIKE 'QA-TEST-%'` | **3** | 3 | **3** (dokunulmadı) |
| `SELECT COUNT(*) FROM hareketler WHERE tenant_id='P16TEST…'` (fişli) | 0 | 2 satır / **1.620,00 TL** (Σ satır = fiş toplamı **TAM**) | **0** |
| `SELECT COUNT(*) FROM cariler WHERE tenant_id LIKE 'P16TEST%'` | 0 | 3 | **0** |
| `SELECT COUNT(*) FROM urunler WHERE tenant_id LIKE 'P16TEST%'` | 0 | 2 | **0** |
| `SELECT COUNT(*) FROM __EFMigrationsHistory` | 55 | 56 | **56** |

**Test kiracısı silindi:** `information_schema`'dan `tenant_id` kolonu olan **58 tablo** bulundu,
hepsinden `P16TESTTENANT…` satırları silindi, ardından `tenants` satırı. `tenants` sayısı
başlangıçtaki **8**'e döndü. **`QA-TEST-*` fikstürlerine dokunulmadı** (3 → 3).

---

## § 9 — DoD SÖZLEŞMESİ (Bertan koşulu, mühür 2026-09-16 12:12)

Bu pakette **yeni varlık/kayıt türü AÇILMADI** — mevcut iki varlığa (`cariler`, `urunler`) alan
eklendi ve mevcut bir uca (satış fişi) iki alan eklendi. Sözleşme yine de tek tek yazılır:

| Varlık / alan kümesi | Düzenleme ucu | Silme / pasife alma | Yetki | Denetim kaydı | Ekran eylemi |
|---|---|---|---|---|---|
| `cariler` +6 KYC alanı | `PATCH /cari/{id}` (**gerçek PATCH**: gönderilmeyen = koru, null = sil) | Cari **pasife alma** zaten var (`DELETE /cari/{id}` = soft) + **aktife alma** yolu var | `CariController` mevcut `[ModulYetkisi]`/`[RolGerekli]` **değişmedi** | `DenetimServisi` mevcut cari yazma kaydı **çağrılır**, yeniden yazılmadı | Düzenleme **çekmecesinde** 6 alan (`#duz-unvan`, `#duz-kimlik-turu`, `#duz-dogum`, `#duz-uyruk`, `#duz-iskonto`, `#duz-segment`) |
| `urunler` +7 kart alanı | `PATCH /urunler/{id}` (**üç durumlu**: yok=koru · null=sil · değer=ata) | Ürün **pasife alma + aktife alma** zaten var | mevcut, değişmedi | mevcut, değişmedi | Düzenleme çekmecesinde 7 alan (`#d-tedarikci` … `#d-min-stok`) |
| Satış fişi `satirIskontoYuzde` | Fiş **kesilmeden önce** satır içi düzenlenir (`#si-kalem-iskonto-{urunId}`) | — | mevcut satış yetkisi | fiş yazma kaydı mevcut | tabloda satır içi düzenleme |
| Satış fişi `fisIskontoYuzde` | Fiş kesilmeden önce `#islem-fis-iskonto` | — | mevcut | mevcut | kutu + önizleme + başarı bandı |

**SALT OKUNUR olanlar ve gerekçesi:**
- **Kesilmiş fişin iskontosu** düzenlenemez: `hareketler` finansal varlıktır, **hard DELETE YASAK**;
  düzeltme yolu **48-A storno motorudur** (ters iade taklidi YAPILMADI, DoD EK 2). Kalem satırındaki
  ayar/milyem/gram/has da düzenlenemez — bunlar **ürün kartından türetilir**, düzeltme yolu ürün
  kartıdır ve ekranda rozetle söylenir.
- Bu pakette **hiçbir finansal varlıkta hard DELETE açılmadı.**

### DoD EK (kurul tur 1) karşılanması
1. **Her düğme ya istek atar ya disabled + gerekçe:** P-16 yeni **düğme eklemedi**; eklenen alanların
   hepsi girdi/seçicidir ve `SatirIciDuzenle`/`Cekmece`'nin **mevcut** kaydet düğmelerine bağlıdır.
   bUnit testleri düğmelerin **gittiği ucu ve gövdeyi** ölçer (varlığını değil).
2. **Tek düzenleme kalıbı:** çok alanlı → `Cekmece` (Urunler/Cariler, mevcut), tek alanlı →
   `SatirIciDuzenle` (satır iskontosu). `Pages/` altında **yeni `ModalPencere` düzenleme kullanımı = 0**;
   ikinci `duzenlenenId` kalıbı açılmadı.
3. **Boş/geçersiz gönderimde alan altı Türkçe mesaj:** satır iskontosu (`AlanHatasi`), fiş iskontosu
   (`AlanHatasi`), TCKN/VKN (`#cari-vergino` + `#duz-vergino`), cari iskontosu, ürün maliyet/marj/KDV/
   min-stok alanları. Sessiz etkisizlik **yok** (canlı adım 4 ve 6, bUnit `Eksik_haneli_TCKN…`).
4. **Yıkıcı eylem onayı:** bu paket yıkıcı eylem **eklemedi**.

### DoD EK 2 (mühür 2) — TEK DOKUNUŞ
**Dokunulan sayfalar:** `Pages/Islemler.razor` · `Pages/Urunler.razor` · `Pages/Cariler.razor`
(üçü de bu dalgada **ilk kez** P-16 tarafından açıldı; `Islemler.razor` §7 zincirinde
P-15 → **P-16** → P-18 sırasındadır ve P-15 ona dokunmamıştı). **Tek geçişte** bitirildi.
Ürün metninde fiyat/teklif/tahsilat ifadesi **üretilmedi**.

---

## § 10 — KİMLİK KAPANIŞ TABLOSU

| Kimlik | Durum | Kanıt |
|---|---|---|
| **Y-4** — Satış kalem satırına ayar/milyem/gram/has + satır ve fiş iskontosu (sunucuda yeniden hesaplanır) | **KAPANDI** | Kod: `Pages/Islemler.razor` kalem tablosu (4 yeni kolon + `#si-kalem-iskonto-*` + `#islem-fis-iskonto`) · `Modules/Satis/IskontoHesabi.cs` (tam dosya) · `Modules/Satis/SatisModulu.cs` `SatisFisi` iskonto bloğu. Test: `Satir_iskontosu_SUNUCUDA_dusulur_defter_toplami_netdir` · `Fis_iskontosu_satirlara_ORANSAL_dagitilir_toplam_TUTAR` · `Kalem_satiri_ayar_milyem_gram_ve_has_GOSTERIR` · `Fis_iskontosu_YUZDE_olarak_FIS_ucuna_gonderilir`. Canlı: adım 3, 4, 13 (+ SQL: Σ satır = 1.620,00 TAM). Mutasyon: M16-1, M16-2, M16-9, M16-10. |
| **Y-26** — Ürün kartının eksik alanları | **KAPANDI (7/7 kolon)** · **çoklu taş satırı DEVREDİLDİ** | Kod: `Entities.cs` `Urun` +7 · `UrunModulu.cs` DTO+Olustur+Guncelle · `Pages/Urunler.razor` `#u-p16` ve `#d-p16` blokları. Test: `Urun_kart_alanlari_YAZILIR_ve_PATCH_ile_DUZELTILIR` · `Alis_maliyeti_girilmeyen_urun_SIFIR_MALIYET_sayilmaz` · `Baska_kiracinin_carisi_TEDARIKCI_olarak_yazilamaz` · `Urun_olusturma_formunda_yedi_kart_alani_VAR_ve_GONDERILIR` · `Urun_duzenleme_cekmecesi_kart_alanlarini_OKUR_ve_PATCH_ile_GONDERIR`. Canlı: adım 10, 11, 12, 14. Mutasyon: M16-6. **Devredilen parçalar:** (a) **çoklu taş satırı** — PLAN'ın migration listesi taş satırı tablosu içermiyor, ayrı varlık ister (kapsam büyütmez diye yapılmadı) → **devredildi: P-00 triyajı / sonraki dalga (Y-42 kuralı)**; (b) **marka/model** — PLAN'ın 7 kolonluk listesinde yok → **devredildi: aynı yer**. |
| **Y-27** — Cari kartı eksikleri (TCKN/VKN, ünvan, segment, doğum tarihi, uyruk, iskonto) | **KAPANDI (6/6 kolon)** | Kod: `Entities.cs` `Cari` +6 · `Kuyumcu.Core/Domain/KimlikNo.cs` · `CariModulu.cs` DTO+Olustur+Guncelle+Listele+Getir · `Pages/Cariler.razor` `#cari-p16` ve `#duz-p16`. Test: `Cari_kimlik_alanlari_YAZILIR_ve_geri_OKUNUR` · `Cari_olustururken_TCKN_hane_kurali_SERVISTE_de_isler` · `Cari_kimlik_turu_beyan_edilmezse_uzunluktan_TURETILIR` · `Cari_PATCH_gonderilmeyen_alani_KORUR_null_geleni_SILER` · 3 bUnit testi. Canlı: adım 6, 7, 8, 9. Mutasyon: M16-5, M16-7. **Sapma S-1** (tür kolonu) raporlanmıştır. |
| **B-31** — Pasif cariler satış listesinde seçilebiliyor | **KAPANDI** | Kod: `Pages/Islemler.razor:1817` civarı `/cari?limit=200&aktif=true` + `#islem-cari-pasif-notu` · `Common/IslemDogrulayici.cs` `CariAktifDogrula` · `SatisModulu.cs` `Satis` ve `SatisFisi` çağrıları. Test: `Pasif_cariyle_SATIS_FISI_REDDEDILIR` · `Pasif_cariyle_TEK_KALEMLI_SATIS_da_REDDEDILIR` · `Aktif_cariyle_satis_ETKILENMEZ` · `Islemler_cari_listesini_YALNIZ_aktif_suzgeciyle_ceker`. Canlı: adım 1, 2. Mutasyon: M16-3, M16-8. Sayaç: `P-16: PASIF_CARI_SATISTA`. |
| **B-69** — Sistemin bildiği değerin kullanıcıdan istenmesi (liste fiyatı, stok adedi) — UX-S21 | **KISMEN** | **Kapanan:** `/islemler` birim fiyat alanı — sunucu teklifinden **zaten doldruluyordu**; eksik olan **kaynağın söylenmesiydi**, `#islem-fiyat-kaynak` rozeti eklendi (test `Birim_fiyat_sunucudan_DOLDURULUR_ve_kaynagi_rozetle_yazar`; canlı `/fiyat/teklif` 200). Ayrıca **cari iskonto oranı** artık kasiyerden istenmiyor, sunucu uyguluyor (M16-4 · canlı adım 5) ve ayar/milyem/gram/has ürün kartından türetiliyor (M16-10). **Kapanmayan:** **stok adedi**nin POS ürün seçicisinde gösterilmesi — `/urunler` yanıtı stok taşımıyor, ayrı uç/sorgu ister ve bu paketin yüzeyini aşar → **devredildi: P-18 (Islemler.razor zincirinin bir sonraki eli)**. B-69'un diğer rotalardaki örnekleri (tekil bulgular `ham/triyaj-kayit.json`) bu paketin yüzeyinde değil → **devredildi: ilgili dalga paketleri**. |

---

## § 11 — KOŞUL KARŞILAMA TABLOSU

| Koşul | Durum | Kanıt |
|---|---|---|
| COMMIT YOK · git/rm/mv yok | ✔ | Yalnız `git status --porcelain` okundu; `add/commit/push/checkout/stash/reset/rm/mv` **çalıştırılmadı** |
| 5199'a dokunulmadı | ✔ | Tüm istekler `localhost:5198`; 5199'a hiç istek gönderilmedi |
| Geçici dosyalar yalnız scratchpad'de | ✔ | `mut-yedek/`, `trx-pP-16/`, log/SQL dosyaları scratchpad altında; proje ağacına `.mutorj`/`.bak`/`.tmp` **bırakılmadı** |
| Sır çıktıya yazılmaz | **✖ bir kez ihlal** | § 2 S-5 — dürüstçe raporlandı, düzeltildi, kurucuya kalem açıldı |
| Migration prosedürü (build yeşil, `--no-build` yok, dosyalar okundu, uygulandı) | ✔ | § 4 |
| Tek yazma yolu `HareketFabrikasi` | ✔ | `Hareketler.Add` **kullanılmadı**; satış fişi mevcut fabrikadan geçer (denetim.sh temiz) |
| `IgnoreQueryFilters`/`Find`/`FindAsync` yok | ✔ | denetim.sh TEMİZ |
| `[RolGerekli]` tam eşleşme · yetki değişmedi | ✔ | Yeni uç açılmadı; mevcut controller yetkileri değişmedi |
| DTO DataAnnotations yalnız controller yolunda → servis içi elle kontrol | ✔ | `CariServisi.Olustur/Guncelle` içinde `KimlikNo.Dogrula` + segment + iskonto elle; `SatisFisi` içinde `IskontoHesabi.YuzdeDogrula`. Test: `Cari_olustururken_TCKN_hane_kurali_SERVISTE_de_isler` |
| PATCH sözleşmesi "yoksa koru, null ise sil" + testi | ✔ | `Cari_PATCH_gonderilmeyen_alani_KORUR_null_geleni_SILER` · `Urun_kart_alanlari_YAZILIR_ve_PATCH_ile_DUZELTILIR` · canlı 8/9/11/12 |
| `Sozlukler.cs` TR+EN, çıplak metin yok | ✔ | § 3.8; `required init` → eksik anahtar derleme hatası |
| Her yeni form alanı için düzenleme yolu (UX-S1/UX-S25/UX-S26) | ✔ | § 9 DoD tablosu |
| Mevcut element id'leri DEĞİŞMEDİ; yeni id'ler `<önek>-<alan>` | ✔ | `islem-*`, `u-*`/`d-*`, `cari-*`/`duz-*`, `si-kalem-iskonto-*` |
| Razor özniteliğinde `{ }` lambda yok · `disabled="false"` tuzağı | ✔ | Eklenen kodda öznitelik içi blok lambda yok; yeni `disabled` özniteliği eklenmedi |
| bUnit sahte API `{}` varsayılanı → gerçekçi yanıt | ✔ | § 5 |
| SVG/CSV sayıları InvariantCulture | ✔ | Milyem `ToString("0.##", InvariantCulture)`; SVG/CSV üretimi bu pakette yok |
| Türkçe JSON gövdesi ayrıştırılarak sınandı | ✔ | Canlı adımlarda `python3 -c json.loads`; `grep` ile göz kararı yapılmadı |
| Yeni CSS'te sabit renk yok, satır içi style yok | ✔ | `.satir--alt-hizali`, `.alan--dar` — renk yok; `SATIR_ICI_STYLE` 154 (tavan indirildi) |
| Boş durum + hata/başarı bandı zorunlu | ✔ | Mevcut `BasariBandi`/`HataBandi` kullanıldı; başarı bandı **sunucunun** iskonto rakamını yazar (UX-S27) |
| Ateşle-unut yan kanal beklenmez | ✔ | Tüm yazma yolları `await` edilir; POST yanıtları okunur |
| `Pages` içinde yeni istemci record açma | ✔ | `BU_PAKETTE_EKLENEN_RECORD = 0` |
| Tip sözleşmesi (ad **ve** tip eşleşir) | ✔ | `iskontoYuzde`/`alisMaliyeti`/`minStok` sunucuda `decimal` → istemcide `decimal?`; `dogumTarihi` `DateOnly?` iki tarafta da |
| Sayaç eklenmeden paket kapanmaz · tavan artırılmaz | ✔ | § 7 |
| Kapsam kilidi (yalnız kendi kimlikleri) | ✔ | Başka paketin kimliğine dokunulmadı; büyüyen işler "devredildi" yazıldı |
| Tam süit 0 başarısız · denetim TEMİZ | ✔ | § 5, § 7 |
| Kendi test kiracısı silindi · `QA-TEST-*` korundu | ✔ | § 8 SQL tablosu |
| Tahmin (540 ajan-dk) | ✔ aşılmadı | Tek oturumda tamamlandı |

---

## § 12 — DEĞİŞEN DOSYALAR

`git status --porcelain | grep -v -E "/(bin|obj)/"` çıktısı depo genelinde çok geniştir (bu proje
büyük ölçüde untracked). **P-16'nın dokunduğu/oluşturduğu dosyalar:**

**Değiştirilen (8):**
- `dotnet/Kuyumcu.Api/Data/Entities.cs`
- `dotnet/Kuyumcu.Api/Common/IslemDogrulayici.cs`
- `dotnet/Kuyumcu.Api/Modules/Satis/SatisModulu.cs`
- `dotnet/Kuyumcu.Api/Modules/Cari/CariModulu.cs`
- `dotnet/Kuyumcu.Api/Modules/Urun/UrunModulu.cs`
- `dotnet/Kuyumcu.Core/I18n/Sozlukler.cs`
- `dotnet/Kuyumcu.Web/Pages/Islemler.razor` · `dotnet/Kuyumcu.Web/Pages/Urunler.razor` · `dotnet/Kuyumcu.Web/Pages/Cariler.razor`
- `dotnet/Kuyumcu.Web/wwwroot/css/app.css`
- `dotnet/Kuyumcu.Contracts/Sayfalar/IslemlerSozlesmeleri.cs` · `…/UrunlerSozlesmeleri.cs` · `…/CarilerSozlesmeleri.cs`
- `dotnet/denetim.sh`

**Yeni (5):**
- `dotnet/Kuyumcu.Core/Domain/KimlikNo.cs`
- `dotnet/Kuyumcu.Api/Modules/Satis/IskontoHesabi.cs`
- `dotnet/Kuyumcu.Api/Data/Migrations/20260921001728_P16UrunCariKartAlanlari.cs` (+ `.Designer.cs`)
- `dotnet/Kuyumcu.Tests/P16SatisKalemiTestleri.cs`
- `dotnet/Kuyumcu.Tests/P16EkranTestleri.cs`
- `dokumanlar/09-uygulama-kayitlari/PAKET-P-16-uygulama-raporu.md` (bu dosya)

`MIMARI-KALAN-PAKETLER-2026-09-15.md`, `PLAN-TEK-SEFER-2026-09-18.md` ve
`SONRAKI-OTURUM-PROMPT.md` dosyalarına **DOKUNULMADI**.

**Depoda bulunan, P-16'ya ait OLMAYAN artıklar (bilgi, silinmedi):**
`dotnet/Kuyumcu.Api/Modules/Satis/SatisModulu.cs.mutorj` · `…/OdemeSepeti.cs.mutorj` ·
`dotnet/Kuyumcu.Tests/AramaHttpTestleri.cs.bak` · `…/MadenDefteriTestleri.cs.bak` ·
`dotnet/denetim.sh.mutbak`. Bunlar önceki paketlerden kalmıştır; hook `rm`'i bloklar,
**silme kurucuya bırakıldı**.

---

## § 13 — AÇIK KALANLAR

1. **B-69 stok adedi** — POS ürün seçicisinde stok görünmüyor; `/urunler` yanıtı stok taşımıyor.
   **Devredildi: P-18** (`Islemler.razor` zincirinin sonraki eli).
2. **Y-26 çoklu taş satırı** ve **marka/model** — PLAN'ın migration listesinde yok, ayrı varlık ister.
   **Devredildi: P-00 triyajı / sonraki dalga (Y-42 kuralı).**
3. **P-15'ten devralınan `DUZ_SELECT_LIMIT`** — `Islemler.razor`'daki 2 `limit=200` açılır listesi
   (cari + ürün) bu pakette **kayıt seçiciye çevrilmedi**: P-16'nın kimliği değil ve kapsam kilidi var.
   `dotnet/duz-select-limit.txt` tavanı **düşürülmedi**. **Devredildi: P-18.**
4. **TCKN sağlama (checksum)** — uygulanmadı; PLAN DoD'u yalnız hane sayısı istiyor. Eski kayıtlar
   arasında sağlamadan geçmeyenler olabilir; açılırsa veri temizliği gerekir.
5. **Fiş iskontosunun FATURA/KDV tarafına yansıması** — bu pakette `hareketler` satır tutarı net
   yazılır; fatura kalemi/KDV matrahı zaten satır tutarını okuduğu için tutarlıdır, ancak
   **"iskonto satırı" olarak ayrı gösterim** (e-Fatura iskonto alanı) yapılmadı.
   **Devredildi: muhasebe/fatura paketi (P-21 / P-26 yüzeyi).**

---

## § 14 — KURUCUYA / MALİ MÜŞAVİRE NOTLAR

1. **K-5 sır rotasyonu acil hâle geldi.** MySQL root parolası `docker-compose.yml`de düz metindir ve
   **iki ardışık pakette** (P-15 S-5, P-16 S-5) yanlışlıkla ajan çıktısına düşmüştür. Bu bir süreç
   hatası değil, **tasarım hatasıdır**: parola dosyada düz durdukça her okuma bir risktir.
   Öneri: parolayı `.env`e taşımak + rotasyon (Mehmet'in yayın vetosu zaten bunu bekliyor).
2. **KDV oranı (mali müşavir).** `urunler.kdv_orani` kolonu açıldı ama **hiçbir oran koda
   yazılmadı**: boş bırakılırsa mevcut özel matrah motoru karar verir. Kuyum ürünlerinde
   **KDV 23/e özel matrah** uygulaması teyidi (**K-10**) gelmeden bu alanın elle doldurulması
   **önerilmez**; ekranda da "boş bırakın, oran ürün tipinden türetilir" yazar.
3. **TCKN doğrulaması (mali müşavir / hukuk).** Yalnız **hane sayısı** denetleniyor. MASAK kimlik
   tespitinde resmî sağlama beklentisi varsa ayrı kalem açılmalıdır; bugün geçersiz ama 11 haneli
   bir numara sisteme girebilir.
4. **Fiş iskontosu ve e-Fatura.** İskonto bugün **satır tutarına gömülerek** deftere yazılıyor.
   e-Fatura/e-Arşiv'de iskontonun **ayrı satır** olarak gösterilmesi isteniyorsa (bazı
   entegratörler ister) bu ayrı bir kalemdir — mali müşavir teyidi gerekir.
5. **Cari iskonto oranı bir TİCARİ taahhüttür.** Kartına oran yazılan cariye, kasiyer hiçbir şey
   girmese bile o indirim **otomatik** uygulanır (UX-S21 gereği). Bu davranış bilinçlidir ve
   ekranda "cari kartındaki orandan" diye yazar; pilot öncesi kurucunun bunu onaylaması iyi olur.
