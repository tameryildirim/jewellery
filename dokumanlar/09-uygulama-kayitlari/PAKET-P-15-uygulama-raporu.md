# PAKET P-15 — KABUK: GENEL ARAMA, KOMUT PALETİ, ŞUBE SEÇİCİ, KAYIT SEÇİCİ, DERİN BAĞLANTI

**Tarih:** 2026-09-21 · **Kimlikler:** Y-29 · Y-5 · B-25 · B-29 · **Dalga:** DALGA 4 · **Sürüm:** 1.0
**Rol:** Taşkın (03 Backend) + Ertuğ (03 Frontend) · **Model:** Opus
**Durum:** **BİTTİ (bir sayaç hedefi KISMEN — § 3'te dürüstçe yazılı)**
**COMMIT YOK** · 5199'a DOKUNULMADI · canlı doğrulama 5198'de yapıldı, süreç durduruldu.

---

## § 0 — ÖNKOŞUL ÖLÇÜMÜ (P-14, P-04)

| Önkoşul | Diskte | Ölçüm |
|---|---|---|
| **P-14** | **VAR** — `dokumanlar/09-uygulama-kayitlari/PAKET-P-14-uygulama-raporu.md` | `Layout/MenuTanimi.cs` mevcut ve menü TEK KAYNAK (38 bağlantı, grup başlıklı). Komut paleti bu kaynaktan beslendi; ikinci bir menü listesi AÇILMADI. |
| **P-04** | **VAR** — `PAKET-P-04-uygulama-raporu.md` + `dotnet/sube-suzgec-siniflandirma.txt` | `IKiraciBaglami.SubeId/TumSubeler` + tek `HasQueryFilter` mekanizması ayakta. Arama ucu kendi kiracı/şube süzgecini KURMADI; kapsamın tabanı P-04 filtresidir. |

Kapsam daraltması gerekmedi: her iki önkoşulun çıktısı da mevcut ve kullanılabilir hâldedir.

---

## § 1 — YAPILAN İŞ

### 1.1 Sunucu — `Modules/Arama/` (YENİ uç)
`GET /api/v1/arama?q=&tur=&limit=&ofset=&sube=`

- **Sözleşme** `Kuyumcu.Contracts/Sayfalar/AramaSozlesmeleri.cs` (YENİ): `AramaTurleri` (tek kaynak, UX-S31),
  `AramaSonucu(Kimlik, Ad, AltBilgi, Tur, Rota)`, `AramaYaniti(..., DahaVar, Tavan, Sonuclar)`.
  Aynı tip HEM sunucuda HEM istemcide kullanılır → ad ve tip ayrışamaz (PLAN § 1 md.9).
- **Türler:** `CARI` · `URUN` · `FIS` · `FATURA` · `BANKA_HAREKET` · `HAREKET`.
- **Sayfalama (Y-5):** `limit` 1..50 arası kelepçelenir; sunucu **bir fazlasını** okuyup `dahaVar`ı
  **sayarak** döndürür — "belki daha vardır" tahmini yoktur (UX-S29, sessiz kırpma yok).
- **Yetki:** sınıf düzeyinde tek `[ModulYetkisi]` YETMEZ (uç çok modül tarar); yetki **tür başına**
  `YetkiServisi.ErisebilirMi` ile sorulur. Tek tür isteyip yetkisi olmayan **403**, genel aramada o tür
  **hiç taranmaz**. Bilinmeyen tür → modül `null` → **fail-closed**.
- **Şube (PLAN riski):** servis elle kiracı/şube süzgeci KURMAZ. `sube` parametresi yalnız **daraltır**;
  şubeye kilitli rol başka şube isterse **403** (sessizce kendi şubesine düşürülmez). `Hareket`
  `ISubeVarlik` olduğu için P-04 global filtresi zaten uygulanır.
  Eklenen tek `SubeId ==` noktası `dotnet/sube-suzgec-siniflandirma.txt`'e **(b)** sınıfıyla yazıldı.
- DI kaydı `Modules/ModulKayit.cs`.

### 1.2 İstemci — üç yeni bileşen
| Dosya | İş |
|---|---|
| `Bilesenler/KayitSecici.razor` (YENİ) | Sunucuda arayan + sayfalayan kayıt seçici. Girdi kimliği **çağıranın verdiği mevcut id**'dir (`ff-fis`, `pos-eslestir-hareket`, `sb-hareket` DEĞİŞMEDİ); türetilen kimlikler `<id>-sonuc-<n>` · `-secili` · `-temizle` · `-ara` · `-durum` · `-hata` · `-daha-var`. |
| `Bilesenler/GlobalArama.razor` (YENİ) | Üst bardaki **genel arama + komut paleti**. "Ekranlar" bölümü `MenuTanimi.Ogeler`den gelir ve **menüyle birebir aynı** yetki + mod koşulundan geçer; "Kayıtlar" bölümü `/arama` (GENEL). |
| `Bilesenler/SubeSecici.razor` (YENİ) | Kabuğun şube kapsamı. Tek şube ya da şubeye kilitli rolde **seçici çizilmez** (UX-S22), salt okunur satır + "oturumdan" rozeti çıkar. Seçim `KabukServisi.AramaSubesi`ne yazılır, arama ucuna `?sube=` olarak gider; son sözü sunucu söyler. |
| `Bilesenler/DerinBaglanti.cs` (YENİ) | **Tek** sorgu dizesi ayrıştırıcısı (B-29). Aynı ad iki kez gelirse İLKİ kazanır; boş değer süzgeç sayılmaz. |

Bağlama: `Layout/UstBar.razor` (yalnız `<GlobalArama />` + `<SubeSecici />` EKLENDİ; mevcut hiçbir id değişmedi).
`Servisler/KabukServisi.cs`: `AramaSubesi` + `AramaSubesiAta` eklendi (görünüm durumu; yetki DEĞİL).
`Bilesenler/Ikonlar.cs`: `Arama = "search"`.
`wwwroot/css/app.css`: P-15 bloğu — **sabit renk yok**, yalnız `var()` belirteçleri; satır içi style yok; 480 px altında panel tam genişlik.

### 1.3 B-25 — ULID elle yazma kalktı
| Ekran | Önce | Sonra |
|---|---|---|
| `Pages/Faturalar.razor` `#ff-fis` | 26 haneli ULID elle yazılıyordu | `KayitSecici Tur=FIS` — cari adı/fiş kimliğiyle arar, **ad + tarih + kalem + tutar** gösterir |
| `Pages/PosTahsilatlar.razor` `#pos-eslestir-hareket` | ULID elle | `KayitSecici Tur=BANKA_HAREKET` |
| `Pages/Uyum.razor` `#sb-hareket` | ULID elle (isteğe bağlı) | `KayitSecici Tur=HAREKET Zorunlu="false"` |

Seçim değişince bağımlı durum **düşürülür** (UX-S24): Faturalar'da önizleme, POS'ta öneri sıfırlanır.

### 1.4 B-29 — derin bağlantı sözleşmesi
- `Pages/Cariler.razor`: `q` · `tur` · `aktif` · `cariId` okunur, **kutulara yazılır**, rozet + "Süzgeci temizle" düğmesi çıkar. `cariId` liste süzgecinin dışında kalırsa ad sunucudan okunup arama kutusuna yazılır ve liste yeniden çekilir; bulunamazsa **hata bandı** (sessiz düşürme yok).
- `Pages/Urunler.razor`: `q` · `ayarId` · `maden` · `kategoriId` · `reyonId` okunur, kutulara yazılır, aynı rozet + temizleme yolu.
- Arama ucunun ürettiği rotalar yalnız **okunan** süzgeçlerdir: `/cariler?cariId=…` ve `/urunler?q=<kod>`.
  Rotası olmayan tür (fiş, fatura, dekont, defter satırı) komut paletinde **tıklanabilir listeye hiç girmez** —
  "tıklanınca hiçbir şey olmayan düğme" doğmaz (DoD EK md.1).

### 1.5 Sözlük (TR + EN)
`Kuyumcu.Core/I18n/Sozlukler.cs` → `SozlukKabuk`a 25 anahtar (arama, seçici, şube kapsamı, tür etiketleri,
derin bağlantı rozeti). `required init` olduğu için **eksik anahtar derleme hatası** verir; çıplak metin yok.
Ham veritabanı kodu ekrana basılmaz: `CARI/URUN/FIS/...` sözlükten çevrilir; sunucu yanıtındaki alt bilgiden
`MUSTERI`, fatura `durum`, banka `yon` gibi ham kodlar **kaldırıldı** (UX-S31).

---

## § 2 — SAPMALAR VE GEREKÇELERİ (Anayasa md.1)

### S-1 — PLAN İÇİ ÇELİŞKİ: `DUZ_SELECT_LIMIT = 0` ile dosya ayak izi aynı anda sağlanamaz
PLAN P-15 bloğu üç şeyi birden söylüyor:
1. DoD: "`limit=200` düz açılır listeler kalkar" · Sayaç: `DUZ_SELECT_LIMIT = 0`
2. DoD: "**`Islemler.razor`'a DOKUNULMAZ**"
3. Dosyalar: `Pages/Faturalar.razor` (**yalnız `#ff-fis` alanı**)

**Bugün taze ölçüm:** `Pages/` altında **12** adet `limit=200` noktası var; **2'si `Islemler.razor`'da**
(satış formunun cari + ürün açılır listeleri) ve maddesi 2 gereği bu paket onlara dokunamaz.
Ayrıca `Faturalar.razor`daki 2 nokta `#ff-fis` DEĞİL (fatura kalem formu, P-16'nın tek eli),
`Urunler.razor` P-16'nın tek elidir. **Yani sıfır, bu paketin yetkisiyle ulaşılamaz.**

**Çözüm (mekanizma korunarak):** sayaç **cırcır** olarak kuruldu, hedef indirilmedi ve tavan yükseltilmedi.
`dotnet/duz-select-limit.txt` her noktayı tek tek sınıflandırır (P-04'ün şube tablosu deseniyle):
`ACIK = 9` · `KAPSAM_DISI = 3` (liste sayfalaması, açılır liste değil) · **toplam 12**.
`denetim.sh` (a) ölçülen nokta sayısının tabloyla **birebir eşitliğini**, (b) `ACIK ≤ 9` tavanını denetler.
Sınıflandırılmamış yeni bir `limit=200` = **DENETİM KIRMIZI**. Tavan yalnız azalır.

**Kimlik tablosunda bu "KISMEN"dir ve devredildiği paket yazılıdır.**

### S-2 — Dosya ayak izi genişletildi (4 dosya), gerekçesiyle
| Dosya | Neden ayak izi dışına çıkıldı |
|---|---|
| `Pages/Cariler.razor` · `Pages/Urunler.razor` | **PLAN DoD'u zorunlu kılıyor:** "sorgu dizesindeki her süzgeç **hedef sayfada uygulanır** ve kutularda görünür". Süzgeci okuyacak yer hedef sayfadır; ayak izi listesi hedef sayfaları saymamış. Değişiklik **eklemelidir** (okuma + rozet + temizleme); P-16'nın ilan ettiği satır aralıklarına (`Cariler 235-284, 317-367` · `Urunler 168-390, 576-800`) **girilmedi**. |
| `Pages/PosTahsilatlar.razor` · `Pages/Uyum.razor` | Sayaç `HAM_ULID_GIRISI = 0` mutlak sıfırdır ve bu iki alan depodaki diğer iki ham ULID girdisidir. Hiçbir paketin tek-el zincirinde değiller (§ 7 haritası). Değişiklik tek elemanın değiştirilmesidir; **id'ler korundu**. |

**Çelişki kaydı (istenen biçimde):** PLAN bloğunun "Dosyalar" satırı ile "DoD/Sayaçlar" satırı çelişmektedir;
talimat gereği **PLAN'ın DoD/sayaç hedefi üstün tutulmuş**, ayak izi asgari ölçüde genişletilmiştir.

### S-3 — Komut paletinde Ctrl+K kısayolu YOK
Palet `#ust-arama-ac` düğmesiyle açılır, `Esc` ile kapanır. Belge düzeyinde klavye kısayolu JS interop
gerektirir; PLAN DoD'u kısayol istemiyor. **Tahmin edilmedi, eklenmedi** — QA turuna bilgi olarak yazıldı.

### S-4 — Şube seçici jetonun şubesini DEĞİŞTİRMEZ
P-04 şubeyi JWT'den okur; şube değiştirmek jeton yeniden üretimi ister ve `Modules/Auth/` **§ 7'de P-09'un
tek elidir**. Bu yüzden seçici, şubeyi **arama kapsamı** olarak besler (`?sube=`), sunucu daraltmayı P-04
bağlamına göre doğrular. "Tüm ekranlarda şube değiştirme" **devredildi: P-09 / sonraki dalga triyajı (Y-42)**.

### S-5 — Sır sızıntısı (dürüstlük kaydı)
Canlı doğrulama hazırlığında `grep -n "MYSQL" docker-compose.yml` komutu ile dosyanın **MySQL root parolasını
içeren satırı çıktıya düştü**. Parola zaten depoda düz metindir (`docker-compose.yml:3` bunu kendisi yazıyor) ve
`.gitignore` dışıdır; yine de **çıktıya basılmaması kuralı ihlal edildi**. Sonraki tüm komutlarda değişken
kullanıldı ve hiçbir yerde tekrar basılmadı. **Kurucuya not:** bu, zaten bekleyen **K-5 sır rotasyonu**
kaleminin aciliyetini artırır; rotasyon yapılana kadar bu parola "bilinen" sayılmalıdır.

### S-6 — Proje ağacında kalan geçici dosya
`dotnet/Kuyumcu.Tests/AramaHttpTestleri.cs.bak` — `sed -i.bak` yan ürünü. **İçeriği `printf '' >` ile boşaltıldı**
(0 bayt), derlemeye girmez. Hook `rm`'i engellediği için **silme kurucuya bırakıldı**.
(Not: `dotnet/denetim.sh.mutbak` ve `Kuyumcu.Tests/MadenDefteriTestleri.cs.bak` bu paketten ÖNCE de vardı.)

---

## § 3 — SAYAÇLAR (denetim.sh `# >>> P-15` … `# <<< P-15` bloğu)

Ölçüm komutları betiğe **yorum olarak** yazıldı; tabanlar belgeden kopyalanmadı, blok içinde **taze** koşuyor (BK-07).

| Sayaç | Hedef | Bugün | Durum |
|---|---|---|---|
| `DUZ_SELECT_LIMIT` | 0 | **açık 9 / toplam 12** (tavan 9, cırcır) | **KISMEN** — § 2 S-1 |
| `SORGU_SUZGECI_OKUMAYAN_SAYFA` | 0 | **0** (taranan rota 4) | ✅ |
| `HAM_ULID_GIRISI` | 0 | **0** (3 → 0) | ✅ |
| `ARAMA_ELLE_KIRACI_SUZGECI` (destek) | 0 | **0** | ✅ |

`SORGU_SUZGECI_OKUMAYAN_SAYFA`ın kanıtı **çağrıdır, tanım değil**: ilk sürüm metot adını arıyordu ve
mutasyon **M15-8** metot dosyada dururken çağrısı silinince sayacın yeşil kaldığını gösterdi — sayaç
`^[[:space:]]*SorgudanSuzgecOku\(\);` / `SupplyParameterFromQuery` çağrısını arayacak biçimde **sertleştirildi**
ve mutasyon tekrarlandı (artık KIRMIZI).

**Diğer paketlerin sayaçları da korundu:** `SAYFA_RECORD_TAVAN` (3) aşılmasın diye `SubeOzeti`
yeniden tanımlanmadı — `Kuyumcu.Core.Sozlesmeler.SubeOzeti` **yeniden kullanıldı** (CONTRACTS_DISI_MODEL = 0).
`P-11 DIS_HATADA_VERI_DUSUREN_YOL_TOPLAM` artmasın diye `SubeSecici`nin `catch` blokları veri düşürmez;
durum `try`dan önce sıfırlanır. `Pages: @onclick'siz <button>` tavanı artmasın diye yeni düğmelerde
`@onclick` `<button>` ile aynı satıra yazıldı.

---

## § 4 — DERLEME · MIGRATION

```
dotnet build Kuyumcu.sln --nologo   →   0 Uyarı, 0 Hata
```

**Migration: YOK ve GEREKMEDİ.** Bu paket şema değiştirmedi; yeni varlık/kolon yaratmadı.
`__EFMigrationsHistory` = **55** (başlangıç 55, son 55). `dotnet ef` hiç çalıştırılmadı.

---

## § 5 — TESTLER

| Koşum | Sonuç |
|---|---|
| `AramaHttpTestleri` (gerçek HTTP) | **14/14 yeşil** |
| `DerinBaglantiSafTestleri` (saf) | **11/11 yeşil** |
| `KabukAramaEkranTestleri` (bUnit) | **18/18 yeşil** |
| `CariEkranTestleri` (bUnit, +3 yeni) | **38/38 yeşil** |
| **TAM SÜİT** `dotnet test … --logger trx` | **3268 test · Geçti 3268 · Başarısız 0 · Atlanan 0** (56,0 sn) |
| `dotnet/denetim.sh` | **DENETİM TEMİZ** |

**Yeni test sayısı: 46** (14 HTTP + 11 saf + 18 bUnit + 3 Cariler derin bağlantı).
Bu pakete girmeden önceki toplam = 3268 − 46 = **3222** (hesaplanmıştır; oturum başındaki `3032` rakamı
2026-09-19 kapısına aittir ve aradaki paketlerle bayatlamıştır — tahmin edilmedi, çıkarıldı).

**Mevcut testlerde yapılan uyarlamalar (susturma DEĞİL, sözleşme değişikliği):**
- `FaturaEkranTestleri`: `#ff-fis`e ULID yazan 4 adım, "ara → sonuçtan seç" adımına çevrildi (B-25 gereği
  davranış değişti); sahte API'ye gerçekçi `/arama` yanıtı eklendi. Testlerin **iddiaları değişmedi**.
- `RotaBaslikTestleri` · `BildirimEkranTestleri`: `UstBar` artık `GlobalArama` barındırdığı için
  `YetkiServisi` kaydı eklendi (eksik DI → 4 test `InvalidOperationException` veriyordu; **gizlenmedi**,
  tam süit çıktısıyla görüldü ve kaynağı giderildi).

---

## § 6 — MUTASYON KANITLARI

Yöntem: kaynak `scratchpad/mut-yedek/P-15/` altına kopyalandı → `perl -0777 -pi -e` ile **tek** değişiklik →
filtreli `dotnet test` → yedek geri kopyalandı → **`cmp` ile birebir aynılık kanıtlandı**.
Son mutasyondan sonra TEMİZ `dotnet build` (0 uyarı / 0 hata).

| Kod | Değişiklik | Kırmızıya dönen test (ADIYLA) | cmp |
|---|---|---|---|
| **M15-1** | `SubeDarallmasiCoz` fail-open: çapraz şube 403'ü kaldırıldı | `AramaHttpTestleri.Sube_kilitli_rol_BASKA_sube_isterse_403_alir` | AYNI |
| **M15-2** | `FisAra`da `sube` daraltması silindi | `AramaHttpTestleri.Tum_subeler_rolu_sube_secici_ile_DARALTIR` | AYNI |
| **M15-3** | Tür başına yetki kapısı açıldı (`erisebilir = true`) | `AramaHttpTestleri.Yetkisiz_turde_tek_tur_aramasi_403_doner` | AYNI |
| **M15-4** | `dahaVar` sondası kaldırıldı (`sayfa+1` → `sayfa`) = sessiz kırpma | `AramaHttpTestleri.Sinira_deginde_dahaVar_TRUE_doner_sessiz_kirpma_yok` | AYNI |
| **M15-5** | Seçili kayıtta ULID ekrana basıldı | `KabukAramaEkranTestleri.Secim_ULIDi_govdeye_tasir_ekranda_AD_gosterir` | AYNI |
| **M15-6** | `KayitSecici` hata yolunda listeyi boşaltıp boş duruma düşürüyor (UX-S28 delindi) | `KabukAramaEkranTestleri.Sunucu_hatasi_bos_duruma_CEVRILMEZ` | AYNI |
| **M15-7** | `DerinBaglanti`: aynı ad ikinci kez **eziyor** (`TryAdd` → `[ad]=`) | `DerinBaglantiSafTestleri.Ayni_ad_iki_kez_gelirse_ILKI_kazanir` | AYNI |
| **M15-8** | `Cariler.razor` sorgu süzgecini OKUMUYOR (çağrı silindi) | `CariEkranTestleri.Baglantidaki_q_suzgeci_sunucuya_gider_ve_rozetle_soylenir` · `…Baglantidaki_tur_suzgeci_KUTUYA_yazilir` **+** `denetim.sh: SORGU_SUZGECI_OKUMAYAN_SAYFA ✗` | AYNI |
| **M15-9** | Rotasız sonuç da tıklanabilir listeye giriyor (DoD EK md.1) | `KabukAramaEkranTestleri.Rotasiz_sonuc_tiklanabilir_listeye_GIRMEZ` | AYNI |
| **M15-10** | Tek şubede de seçici çiziliyor (UX-S22 delindi) | `KabukAramaEkranTestleri.Tek_subede_secici_CIZILMEZ_salt_okunur_satir_cikar` | AYNI |

### İlk turda bulunan **2 TEST AÇIĞI** ve kapatılması
1. **M15-8 ilk turda hiçbir şeyi kırmadı** — `denetim.sh` metot **adını** arıyordu, çağrıyı değil.
   Sayaç sertleştirildi (§ 3) **ve** `CariEkranTestleri`ne 3 derin bağlantı testi eklendi; mutasyon
   tekrarlandı → 2 test + denetim KIRMIZI.
2. **M15-9 ilk turda hiçbir şeyi kırmadı** — bUnit sahte API'nin tüm sonuçları rotalıydı.
   Sahte API'ye **rotasız** bir sonuç eklendi ve test onun çizilmediğini iddia edecek şekilde güçlendirildi;
   mutasyon tekrarlandı → KIRMIZI.

---

## § 7 — CANLI DOĞRULAMA (localhost:**5198** — 5199'a DOKUNULMADI)

API: `ASPNETCORE_ENVIRONMENT=Development dotnet run --project Kuyumcu.Api --no-build --no-launch-profile
--urls http://localhost:5198` (PID 89582 saklandı, iş sonunda `kill`).
Kimlik: test kiracısı SQL ile kuruldu; jeton, yapılandırmadaki imza anahtarıyla **yerel olarak üretildi**
(anahtar değişkende tutuldu, **hiçbir yerde basılmadı**). Yanıt gövdeleri **okundu** (`-o /dev/null` kullanılmadı).

| # | Adım | Beklenen | Gerçek |
|---|---|---|---|
| 1 | `GET /arama?q=ab` **jetonsuz** | 401 | **401** |
| 2 | SAHIP genel arama `q=P15 Zeynep` | 200, cari + 2 fiş | **200**, `sayi=3`; cari rotası `/cariler?cariId=…`, fiş rotası `null` |
| 3 | TEZGAHTAR `tur=FIS&q=P15FIS` | yalnız kendi şubesi | **200**, `sayi=1` → yalnız `P15FIS-MERKEZ` (komşu şube fişi **YOK**) |
| 4 | TEZGAHTAR `&sube=<komşu>` | **403** | **403** `"Başka bir şubenin kayıtlarında arama yapamazsınız."` |
| 5 | SAHIP `&sube=<komşu>` | daraltır | **200**, `sayi=1` → yalnız `P15FIS-KOMSU` |
| 6 | `q=P` (1 karakter) | 200 + boş | **200**, `sayi=0`, `tavan=50` (hata DEĞİL) |
| 7 | `tur=UYDURMA` | 400 | **400** `"Bilinmeyen arama türü: UYDURMA"` |
| 8 | `tur=CARI&limit=1` | `dahaVar=true` | **200**, `dahaVar=true` |
| 9 | Pasif cari | işaretli döner, gizlenmez | **200**, `altBilgi: "05551110002 · pasif"` |
| 10 | `tur=HAREKET&q=P15 merkez` | defter satırı | **200**, `sayi=1`, ad = açıklama (ULID değil) |
| 11 | Kiracı izolasyonu: `q=Ay&tur=CARI` (diğer kiracılarda "Ayşe" var) | 0 sonuç | **200**, `sayi=0` |
| 12 | `GET /` · `GET /css/app.css` | 200 · P-15 sınıfları | **200** · **11 eşleşme** (`kayit-secici`/`kabuk-arama`/`sube-secici`) |
| 13 | `_framework/Kuyumcu.Web.wasm` dizge taraması | yeni kimlikler var | `ust-arama-ac` **True** · `ust-sube-secici` **True** · `kabuk-arama-paneli` **True** · `/arama?q=` **True** · `DerinBaglanti` **True** |
| 14 | API durduruldu | `lsof -nP -iTCP:5198 -sTCP:LISTEN` boş | **0 satır** |
| 15 | 5199 | dokunulmadı | **0 satır** (kurucunun sunucusu zaten ayakta değildi; hiç istek gönderilmedi) |

### SQL kanıtı (`docker exec kuyumcu-mysql mysql … kuyumcu_net`, parola değişkenden)

| Sorgu | Başlangıç | Test sırasında | Son |
|---|---|---|---|
| `SELECT COUNT(*) FROM tenants` | **8** | 9 | **8** |
| `… WHERE ad LIKE 'PAKET%'` | 0 | 1 (`PAKET P-15-TEST`) | **0** |
| `SELECT COUNT(*) FROM cariler WHERE tenant_id='P15T…TENANT'` | 0 | 2 | **0** |
| `SELECT COUNT(*) FROM hareketler WHERE tenant_id='P15T…TENANT'` | 0 | 2 | **0** |
| `… WHERE ad LIKE 'QA-TEST-%'` | **3** | 3 | **3** (dokunulmadı) |
| `SELECT COUNT(*) FROM __EFMigrationsHistory` | **55** | 55 | **55** |

**Test kiracısı silindi:** `information_schema`dan `tenant_id` kolonu olan **58 tablo** bulundu ve hepsinden
`P15T…TENANT` satırları silindi, ardından `tenants` satırı. `tenants` sayısı başlangıçtaki **8**'e döndü.
`QA-TEST-*` fikstürlerine **dokunulmadı**.

---

## § 8 — DoD SÖZLEŞMESİ (Bertan koşulu)

Bu paket **yeni varlık / kayıt türü YARATMADI** — tek yeni uç **salt okurdur** (`GET /api/v1/arama`),
deftere yazmaz, hiçbir kayıt oluşturmaz/değiştirmez/silmez.

| Varlık / kayıt | Düzenleme yolu | Not |
|---|---|---|
| (yeni varlık yok) | — | Migration YOK, `HareketFabrikasi` ve `StornoServisi` yollarına dokunulmadı |
| `KabukServisi.AramaSubesi` | Şube seçicinin kendisi (aynı ekranda ileri-geri) | **Kalıcı değil**, sunucuya yazılmaz; bir görünüm tercihidir, denetim kaydı gerektirmez |
| Kayıt seçicinin seçimi | `#<id>-temizle` düğmesi | Geri dönüş yolu her seçimde vardır (UX-S25) |
| Derin bağlantı süzgeci | "Süzgeci temizle" düğmesi (`#cari-suzgec-temizle`, `#urun-suzgec-temizle`) | Rozet süzgecin nereden geldiğini söyler |

**Salt okunur olanlar ve nedeni:** arama sonuçlarının kendisi (cari, ürün, fiş, fatura, dekont, defter satırı)
bu ekranda **düzenlenmez**; her biri kendi ekranında düzenlenir ve rotası olan sonuç oraya götürür.
Rotası olmayan türler (fiş, fatura, dekont, defter satırı) **tıklanabilir gösterilmez** — düzenleme yolları
kendi ekranlarındadır (P-12 defter düzeltme, P-16 fatura/satış). Denetim kaydı (`DenetimServisi.YazmaKaydet`)
**çağrılmadı ve çağrılmamalıdır**: yazma yok.

---

## § 9 — KİMLİK KAPANIŞ TABLOSU (zorunlu)

| Kimlik | Durum | Kanıt |
|---|---|---|
| **Y-5** — Sunucu taraflı aranabilir seçici bileşeni | **KAPANDI** | `Kuyumcu.Web/Bilesenler/KayitSecici.razor` (YENİ) + `Kuyumcu.Api/Modules/Arama/AramaModulu.cs:52` `Ara(...)` sayfalı/limitli. Test: `AramaHttpTestleri.Sinira_deginde_dahaVar_TRUE_doner_sessiz_kirpma_yok`, `…Ofset_sonraki_sayfayi_getirir`, `KabukAramaEkranTestleri.Yazinca_sunucuda_ARANIR_ve_sonuclar_cizilir`. Canlı: § 7 adım 2, 8. **Mutasyon M15-4.** |
| **Y-29** — Kabuk katmanı: arama, komut paleti, şube seçici | **KAPANDI** | `Bilesenler/GlobalArama.razor` + `Bilesenler/SubeSecici.razor` (YENİ), `Layout/UstBar.razor`'a bağlandı. Test: `KabukAramaEkranTestleri.Palet_acilinca_girdi_cikar_ve_kapatilabilir`, `…Palette_ekran_arama_menu_kaynagindan_gelir`, `…Tek_subede_secici_CIZILMEZ_salt_okunur_satir_cikar`, `…Secilen_sube_kabuk_durumuna_yazilir_ve_arama_ucuna_gecer`. Canlı: § 7 adım 13 (wasm'de `ust-arama-ac`, `ust-sube-secici`, `kabuk-arama-paneli`). **Mutasyon M15-10.** *(Ctrl+K kısayolu yok — § 2 S-3; şube DEĞİŞTİRME jetonu P-09'a devredildi — § 2 S-4.)* |
| **B-25** — "Fişten Fatura Kes" ULID → kayıt seçici | **KAPANDI** | `Pages/Faturalar.razor` `#ff-fis` → `<KayitSecici Tur="FIS">`; ayrıca `PosTahsilatlar.razor#pos-eslestir-hareket` ve `Uyum.razor#sb-hareket`. Sayaç `HAM_ULID_GIRISI = 0` (3 → 0). Test: `KabukAramaEkranTestleri.Sonuc_listesinde_ULID_gorunmez`, `…Secim_ULIDi_govdeye_tasir_ekranda_AD_gosterir`, `FaturaEkranTestleri.Fisten_kaydetme_POST_faturalar_fisten_ucuna_gider_ve_govdede_fisId_tasir`. Canlı: § 7 adım 2, 10. **Mutasyon M15-5.** |
| **B-29** — Derin bağlantı / süzgeç sözleşmesi | **KAPANDI** | `Bilesenler/DerinBaglanti.cs` (tek ayrıştırıcı) + `Pages/Cariler.razor` ve `Pages/Urunler.razor` okuma/rozet/temizleme. Sayaç `SORGU_SUZGECI_OKUMAYAN_SAYFA = 0` (taranan rota 4). Test: `CariEkranTestleri.Baglantidaki_q_suzgeci_sunucuya_gider_ve_rozetle_soylenir`, `…Baglantidaki_tur_suzgeci_KUTUYA_yazilir`, `…Sorgusuz_acilista_baglanti_rozeti_CIKMAZ`, `DerinBaglantiSafTestleri` (11). Canlı: § 7 adım 2 (`rota` alanı). **Mutasyon M15-7, M15-8.** |

**"1-A adet=1 sözleşmesi mutasyonla yeniden kanıtlanır" (PLAN DoD 2. madde):** bu sözleşme
`Islemler.razor` / satış kalemi tarafındadır ve bu paket **`Islemler.razor`'a dokunmaz**; ilgili
mevcut testler tam süitte (3268/3268) yeşildir ama **bu pakette ayrıca mutasyonla sınanmadı**.
**DEVREDİLDİ: P-16** (satış kalemi tek eli). Gizlenmedi.

---

## § 10 — KOŞUL KARŞILAMA TABLOSU

| Koşul | Durum | Kanıt |
|---|---|---|
| Plan bloğu (Dosyalar/DoD/Sayaçlar) okundu, çelişki raporlandı | ✅ | § 2 S-1, S-2 |
| `limit=200` düz açılır listeler kalkar / `DUZ_SELECT_LIMIT=0` | ⚠️ **KISMEN** | § 2 S-1 · § 3 · `dotnet/duz-select-limit.txt` |
| Seçici sunucuda arar ve sayfalar | ✅ | § 9 Y-5 |
| "Fişten Fatura Kes" ad + tip gösteren kayıt seçici | ✅ | § 9 B-25 |
| 1-A adet=1 mutasyonla yeniden kanıt | ❌ **DEVREDİLDİ: P-16** | § 9 sonu |
| Derin bağlantı sözleşmesi | ✅ | § 9 B-29 |
| Şube seçici P-04 bağlamını besler, elle süzgeç geri gelmez | ✅ | `ARAMA_ELLE_KIRACI_SUZGECI=0`; `SubeId ==` noktası sınıflandırma tablosunda; § 7 adım 3-5 |
| `Islemler.razor`'a DOKUNULMADI | ✅ | Dosya değiştirilmedi (§ 11) |
| Sayaçlar denetim.sh'a CIRCIR eklendi, ölçüm komutu yorumda, taban taze | ✅ | § 3 · `denetim.sh` `# >>> P-15` |
| Tavan/eşik artırılmadı | ✅ | `ACIK ≤ 9`, diğer sayaçlar mutlak 0 |
| Pages içinde yeni istemci record açılmadı | ✅ | `SAYFA_RECORD_TAVAN = 3` korundu; `SubeOzeti` Core'dan yeniden kullanıldı |
| Her iş kuralı için hatayı yeniden üreten test | ✅ | § 5 |
| Mutasyon kanıtı (kır → adıyla kırmızı → geri al → cmp) | ✅ | § 6 (10 mutasyon, 2 test açığı bulundu ve kapatıldı) |
| 5198'de canlı doğrulama (HTTP + SQL), 5199'a dokunulmadı | ✅ | § 7 |
| `dotnet/denetim.sh` TEMİZ | ✅ | § 5 |
| Tam süit 0 başarısız | ✅ | 3268/3268 |
| Test kiracısı silindi, `QA-TEST-*` korundu | ✅ | § 7 SQL tablosu |
| Migration: HAYIR | ✅ | § 4 |
| COMMIT YOK / git komutu yok / rm-mv yok | ✅ | Yalnız `git status --porcelain` okundu |
| Sözlük TR + EN, çıplak metin yok | ✅ | § 1.5 |
| Her düğme ya istek atar ya disabled + sebep (DoD EK md.1) | ✅ | `KabukAramaEkranTestleri.Kisa_sorguda_Ara_dugmesi_KAPALI_ve_sebebi_YAZILI` · `…Rotasiz_sonuc_tiklanabilir_listeye_GIRMEZ` |
| Boş durum + hata/başarı bandı; hata "kayıt yok"a çevrilmez | ✅ | `…Sunucu_hatasi_bos_duruma_CEVRILMEZ` · `…Gercekten_bos_sonucta_BOS_DURUM_cikar_hata_bandi_cikmaz` |
| Yeni CSS'te sabit renk yok, satır içi style yok | ✅ | `app.css` P-15 bloğu (yalnız `var()`) |
| Mevcut element id'leri değişmedi; yeni id'ler `<önek>-<alan>` | ✅ | `ff-fis`, `pos-eslestir-hareket`, `sb-hareket` korundu |
| Razor özniteliğinde bloklu lambda yok; `disabled="@(bool)"` | ✅ | Adlandırılmış metotlar (`Degistir`, `AramayiCalistir`, …) |
| bUnit sahte API yeni uca gerçekçi yanıt | ✅ | `FaturaEkranTestleri` + `KabukAramaEkranTestleri` `/arama` dalları |
| Sır/parola çıktıya yazılmaz | ❌ **BİR KEZ İHLAL** | § 2 S-5 (dürüstlük kaydı + kurucuya not) |
| Tahmin: 360 ajan-dk | Aşıldı sayılmaz — tek oturumda tamamlandı; sapma ölçülmedi (kronometre tutulmadı, **uydurulmadı**) | — |

---

## § 11 — DEĞİŞEN DOSYALAR

`git status --porcelain` bu depoda ayırt edici değildir (tek commit var, dosyaların çoğu zaten `??`).
Bu paketin **fiilen** dokunduğu dosyalar:

**YENİ (7)**
```
dotnet/Kuyumcu.Contracts/Sayfalar/AramaSozlesmeleri.cs
dotnet/Kuyumcu.Api/Modules/Arama/AramaModulu.cs
dotnet/Kuyumcu.Web/Bilesenler/KayitSecici.razor
dotnet/Kuyumcu.Web/Bilesenler/GlobalArama.razor
dotnet/Kuyumcu.Web/Bilesenler/SubeSecici.razor
dotnet/Kuyumcu.Web/Bilesenler/DerinBaglanti.cs
dotnet/duz-select-limit.txt
```
**YENİ TEST (3)**
```
dotnet/Kuyumcu.Tests/AramaHttpTestleri.cs
dotnet/Kuyumcu.Tests/DerinBaglantiSafTestleri.cs
dotnet/Kuyumcu.Tests/KabukAramaEkranTestleri.cs
```
**DEĞİŞEN (14)**
```
dotnet/Kuyumcu.Api/Modules/ModulKayit.cs            (DI kaydı)
dotnet/Kuyumcu.Core/I18n/Sozlukler.cs               (SozlukKabuk +25 anahtar TR+EN)
dotnet/Kuyumcu.Web/Layout/UstBar.razor              (GlobalArama + SubeSecici bağlandı)
dotnet/Kuyumcu.Web/Servisler/KabukServisi.cs        (AramaSubesi)
dotnet/Kuyumcu.Web/Bilesenler/Ikonlar.cs            (Arama ikonu)
dotnet/Kuyumcu.Web/Pages/Faturalar.razor            (#ff-fis → KayitSecici)
dotnet/Kuyumcu.Web/Pages/PosTahsilatlar.razor       (#pos-eslestir-hareket → KayitSecici)
dotnet/Kuyumcu.Web/Pages/Uyum.razor                 (#sb-hareket → KayitSecici)
dotnet/Kuyumcu.Web/Pages/Cariler.razor              (derin bağlantı okuma + rozet)
dotnet/Kuyumcu.Web/Pages/Urunler.razor              (derin bağlantı okuma + rozet)
dotnet/Kuyumcu.Web/wwwroot/css/app.css              (P-15 bloğu)
dotnet/denetim.sh                                   (# >>> P-15 … # <<< P-15)
dotnet/sube-suzgec-siniflandirma.txt                (+1 satır, sınıf b)
dotnet/Kuyumcu.Tests/FaturaEkranTestleri.cs · CariEkranTestleri.cs · RotaBaslikTestleri.cs · BildirimEkranTestleri.cs  (test uyarlaması)
```

**Dokunulan sayfalar (DoD EK 2 — TEK DOKUNUŞ kaydı):**
`Faturalar.razor` · `PosTahsilatlar.razor` · `Uyum.razor` · `Cariler.razor` · `Urunler.razor` ·
`Layout/UstBar.razor`. **`Islemler.razor`'a DOKUNULMADI.**

---

## § 12 — AÇIK KALANLAR

1. **`DUZ_SELECT_LIMIT` 9 açık nokta** — `duz-select-limit.txt`te tek tek yazılı.
   `Islemler.razor` (2) ve `Urunler.razor` (1) → **P-16**; `Faturalar.razor` (2) → **P-16**;
   `Banka.razor` · `BankaEkstre.razor` · `Evraklar.razor` · `Kurulum.razor` (4) → **Y-42 triyajı**.
2. **1-A "adet=1" mutasyon tekrarı** → **P-16**.
3. **Şube DEĞİŞTİRME (jeton yeniden üretimi)** → **P-09 / Y-42 triyajı**. Bugün seçici yalnız
   arama kapsamını daraltır.
4. **Ctrl+K komut paleti kısayolu** — JS interop gerektirir, PLAN istemiyor; QA turuna bilgi.
5. **`Fatura` / `BankaHareket` / `Hareket` / `Fis` için derin bağlantı rotası yok** — hedef sayfalarda
   tek-kayıt açan bir süzgeç bulunmadığı için bilinçli olarak `null` bırakıldı ve bu türler komut
   paletinde tıklanabilir gösterilmiyor. Rotalandırılmaları → **Y-42 triyajı**.
6. **`dotnet/Kuyumcu.Tests/AramaHttpTestleri.cs.bak`** (0 bayt) — hook `rm`'i engelliyor, **silme kurucuda**.
7. **Tarayıcıda göz doğrulaması** (palet görünümü, 375 px) → **QA turu**. Blazor WASM'de kabuk
   istemcide çizilir; curl ile görülmez, tahmin edilmedi.

---

## § 13 — KURUCUYA / MALİ MÜŞAVİRE NOTLAR

- **K-5 (sır rotasyonu) aciliyeti arttı.** § 2 S-5: MySQL root parolası bu oturumun çıktısına bir kez düştü.
  Parola zaten depoda düz metin; rotasyon yapılana kadar "bilinen" sayılmalıdır. **Mali/hukuki hüküm içermez.**
- **Mevzuat hükmü yok.** Bu paket hiçbir vergi/MASAK/KMTS hükmü kodlamadı; mali müşavire soru **yoktur**.
- **Silinecek artık dosya:** `dotnet/Kuyumcu.Tests/AramaHttpTestleri.cs.bak` (0 bayt) — K-4 listesine eklenebilir.
