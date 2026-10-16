# KUYUMCU ERP — TEK SEFERLİK BİTİRME PLANI (v2, MÜHÜRLÜ)

**Tarih:** 2026-09-18 · **Mühürleyen:** Tamer Bey (Patron / Orkestratör)
**Kapsam:** hem eski borçlar hem 2026-09-18 raporlarından gelen yeni işler
**Kurucu talimatı:** *"adam gibi tek seferde, sonradan 'aa bunu unutmuşum' demeyeceğin bir plan"*

---

## § 0 — TEK SAYFA ÖZET (sade dil)

### Ne kadar iş var
**158 iş kalemi** var. Hepsinin bir kimliği var ve hepsi bu planda bir yere yazılı:

- **58 eski borç** (B-1 … B-58) — QA turlarında ve denetimlerde bulunmuş, kapanmamış kusurlar
- **42 yeni iş** (Y-1 … Y-42) — bugünkü ekran-alan doğrulama ve kıyas raporlarından çıkan eksikler
- **19 karar** (R-1 … R-19) — ekip oyuyla kapanacak sürüm/kapsam kararları
- **13 dondurulmuş** (D-1 … D-13) — 1.1 sürümüne bırakılanlar
- **26 kurucu kalemi** (K-1 … K-26) — ekibin yapamayacağı, dışarıya/size bağlı işler

> **Dürüstlük notu (Anayasa md.1):** önceki taslak "147 kalem" diyordu. Hasım turu 11 kalem daha çıkardı (B-55…B-58, Y-40…Y-42, K-23…K-26). Sayı **147 → 158** oldu. Bu bir gerileme değil, kapsamın ilk kez dürüst sayılmasıdır.

### Kaç dalga, ne kadar sürer
**9 dalga, 36 paket.**

> **Düzeltme:** taslak "37 paket" diyordu; paketleri tek tek saydım, **36**. (P-00…P-35)

| Ölçü | Taslakta yazan | Bu planda ölçülen | Neden değişti |
|---|---|---|---|
| Ajan-saat | 303 | **362,1** | Taslak her kapıya 1 QA turu bütçelemişti, oysa kendi kuralı 2 tur istiyor. Eksik: 8 dalga × 8 saat = 64 saat. Yeni rakam § 9 formülünden hesaplandı, tahmin edilmedi. |
| Takvim | 10-12 hafta | **11-15 hafta** | Yukarıdaki 64 saatin takvime yansıması. |
| Paket | 37 | **36** | Sayım hatası düzeltildi. |

**Takvim şartı:** bu rakamlar **iCloud eşitlemesi KAPALIYKEN** geçerlidir (K-20). Açıkken tek derleme 30 dakika ölçülmüştü; o hâlde takvim geçersizdir ve yeniden ölçülür.

### Ne zaman "satışa hazır" denebilir
Üç şart birlikte sağlandığında, öncesinde değil:

1. **İki ardışık temiz QA turu** — 42 rotanın hepsi taranmış, kritik hata = 0 ve düzenlenemeyen veri girişi = 0; ikinci tur **hiçbir kod değişikliği yapılmadan** koşmuş olacak. (Kurul tur 4 mührü, md.1)
2. **Para tahsili ayağı çalışıyor** — abonelik + plan kapısı (P-27) ve kiracı başına ölçüm (P-28) canlı kanıtlı.
3. **Pilot ölçümü dönmüş** — kısıtları yazılı ücretsiz pilot 4-6 hafta koşmuş, destek dakikası gerçek veriyle ölçülmüş (P-32).

Yani: **kod hattı 11-15 hafta**, üstüne pilot penceresi paralel koşar. "Ücretli tarih" cümlesi ancak 1 + 2 + 3 tamamlandıktan sonra kurulabilir.

### Sizden ne bekleniyor
Ekip hiçbir karar için size çıkmıyor; kararları oyla alıyor. Sizden **yalnızca şunlar** bekleniyor:

| Öncelik | Kalem | Ne zaman gerekli | Gelmezse ne olur |
|---|---|---|---|
| 1 | **K-20** iCloud eşitlemesini kapatmak | Hemen, DALGA 0'dan önce | Tüm takvim 2-3 katına çıkar, plan geçersiz |
| 2 | **K-5** sır rotasyonu (JWT, MySQL parolası) | DALGA 1 öncesi | Mehmet yayını fail-closed durdurur |
| 3 | **K-1** canlı veritabanı temizliği izni | DALGA 7 öncesi | FK paketi (P-34) ve temiz demo başlayamaz |
| 4 | **K-10** mali müşavir teyidi (KDV 23/e, KMTS) | DALGA 6 öncesi | Muhasebe köprüsü yarım kalır, "hariç" yazılır |
| 5 | **K-13** imalat/fire sınırı kararı | DALGA 5 öncesi | Hurda satışı paketi bekler |
| 6 | **K-16** rakamlı fiyat ilanı | En sonda | Ürün hazır olur ama fiyat ilan edilemez |
| — | Kalan 20 kurucu kalemi | § 4'teki tabloda | Her birinin karşısında yazılı |

**Tek imza işi:** pilot davetinin gönderilmesi (K-19), sözleşme imzası, para/harcama. Metinleri ekip hazırlar, gönderimi siz yaparsınız.

### Bu plan neden "tek seferlik"
Çünkü kapsamı kapalı bir liste değil, **denetlenebilir bir rejim** olarak kuruyor:

- **P-00** her kimliğin kaynağını tek tek yazar (`KAPSAM-ENVANTERI.md`). Kaynağı bulunamayan kimlik "bu oturumda sentezlendi" diye işaretlenir — gizlenmez.
- **Y-42 triyaj kuralı:** her QA turundan sonra çıkan yeni bulgu 24 saat içinde kimliklenir ve dört kutudan birine yazılır (mevcut pakete ek · sonraki dalgada yeni paket · 1.1 · gerekçeli kapsam dışı). **Damgasız bulgu kapıyı kırar.**
- **Kapsam freni:** yeni bulgu üretimi düşmüyorsa kapsam kurulu toplanır ve 1.0 sınırı yeniden çizilir (Ramazan'ın K1 koşulu).

Böylece "aa bunu unutmuşum" demek mekanik olarak imkânsız hâle gelir: unutulan şey ya envanterde görünür ya kapıyı kırar.

---

## § 1 — İLKELER (bağlayıcı)

1. **MEKANİZMA ÖNCE.** Düzenlenebilirlik, tip sözleşmesi, şube süzgeci, kültürsüz biçim, 375 px mobil genişlik gibi kurallar ekran ekran değil **şablon / bileşen / sayaç** düzeyinde kapatılır ve `dotnet/denetim.sh`'a cırcır (geri dönüşsüz) sayaç olarak bağlanır. *Sayacı olmayan mekanizma paketi DoD'u geçemez.*

2. **SAYAÇ TABANI ÖLÇÜLÜR, KOPYALANMAZ** (Anayasa md.1). Taslağın "49 düzenlenemeyen", "51 elle şube süzgeci", "47 razor" rakamları bayattı. **Bugün depoda ölçtüklerim:**

   | Ölçüm | Değer | Komut |
   |---|---|---|
   | `Pages/` altındaki .razor | 42 | `ls Pages/*.razor` |
   | `^@page` direktifi olan dosya | 42 | `grep -rlE '^@page' --include='*.razor'` |
   | Toplam `^@page` satırı (rota) | 42 | `grep -rhE '^@page'` |
   | `Bilesenler/` dosya | 27 | `ls` |
   | `Layout/` dosya | 5 | `ls` |
   | Modules'te `SubeId ==` | 63 | `grep -rn "SubeId =="` |
   | `HasIndex` | 135 | `grep -c` |
   | `HasOne` / `WithMany` | 0 | `grep -c` |
   | `IgnoreQueryFilters` | 4 | `grep -rn` |
   | Migration .cs | 107 | `ls Migrations/*.cs` |
   | 0 baytlık migration .cs | 8 | `find -size 0` |
   | `denetim.sh:293` BEKLENEN_VARLIK | 51 | dosyadan |
   | `denetim.sh:310` SAYFA_RECORD_TAVAN | 129 | dosyadan |
   | `denetim.sh:706` RAPOR_MAT_BEKLENEN | 43 | dosyadan |
   | `denetim.sh:729` KULTURSUZ_TAVAN | 44 | dosyadan |
   | `Pages/` ham `record ` grep | 157 | `grep -rh 'record '` |
   | `window.print` noktası | 3 | `grep -rn` |

   **YENİ ÖLÇÜM BULGUSU (bugün, hasım turunda yoktu):** `Pages/` altında **5 adet `.mutorj` mutasyon artığı** duruyor (Fiyatlandirma, Islemler, Urunler, VadeTakibi, Sayim) ve bunlar da `@page` içeriyor. Bu yüzden `grep -rl "@page" Pages/` **47** döner — taslaktaki "47 razor" iddiasının kaynağı büyük olasılıkla budur. Artıklar grep tabanlı her sayacı kirletir. **P-02'nin ilk işi:** tüm sayaç greplerine `--include='*.razor'` zorunluluğu getirmek ve `MUTORJ_ARTIK` sayacını kurmak (silme K-4 ile kurucudadır, hook `rm`'i bloklar).

3. **İKİ ARDIŞIK TEMİZ TUR.** Her dalga kapısı; kritik = 0 ve düzenlenemeyen = 0 görülen turdan sonra **değişiklik yapılmadan koşan ikinci bir doğrulama turu** ister. Tek temiz tur kapıyı açmaz. Her dalgaya ikinci tur bütçelenmiştir (≈8 ajan-saat/tur).

4. **TEK SENARYO.** Taslağın "varsayılan Senaryo A"sı kaldırıldı. Kurul mührü md.2/3 gereği F-1 + F-3 (para tahsili, plan kapısı) ve F-4 (telemetri) **koşulsuz 1.0'dadır**; onsuz ürün hiçbir senaryoda satılamaz. Kurucuya kalan tek ticari karar rakamlı fiyat **ilanıdır** (K-16).

5. **TRİYAJ (INTAKE) KURALI — Y-42.** Her QA turundan sonra yeni bulgu 24 saat içinde kimliklenir; {mevcut pakete ek · sonraki dalgada yeni paket · 1.1 · gerekçeli kapsam dışı} kümesinden birine ekip oyuyla yazılır ve `ham/surum.json`'a damgalanır. Damgasız bulgu kapı kırar.

6. **İZLENEBİLİRLİK.** Kimlik kataloğu bugüne dek hiçbir kalıcı dosyada yoktu (hasım bulgusu, doğrulandı). P-00 `KAPSAM-ENVANTERI.md`yi yazar: her kimlik → kaynak (rapor §, dosya:satır, ya da "bu oturumda sentezlendi"). **Bu belge olmadan hiçbir dalga başlamaz.**

7. **YAZ-SERİ YALNIZ KOD İÇİN.** Dalga içinde kod paketleri seri koşar, aynı dosyaya iki el değmez. Belge / pazarlama / pilot paketleri **ayrı hatta paralel** koşar (kurul sonraki_adimlar md.5).

8. **TEK YAZMA YOLU DOKUNULMAZ.** Her yeni hareket ailesi `HareketFabrikasi.Ekle` üzerinden yazılır; geri alma yalnız `StornoServisi` / `StornoSuzgeci`'nden geçer; finansal varlıkta **hard DELETE yoktur**, pasif/durum alanı kullanılır.

9. **SÖZLEŞME TEK YÖNDE.** `Pages` record tavanı yalnız **azalır** ve kapsamı `Pages + Bilesenler + Layout`'a genişler (record'ları Bilesenler'e taşıyarak sayaç oyunlamak imkânsız). Yeni uç önce `Kuyumcu.Contracts` tipini alır; ad **ve tip** yansımayla testte kilitlenir; PATCH'te gönderilmeyen alan = değişmedi.

10. **MIGRATION GERİ ALINABİLİR.** Her `Down()` yazılır **ve en az bir kez gerçekten koşturulur**; geri doldurma yapan migration önce salt okunur etki ölçümü raporlar; boş migration ve `--no-build` **yasak**. 8 adet 0 baytlık .cs bugün duruyor, sayı artamaz.

11. **ŞEMA ÖNCE, EKRAN SONRA, FK EN SONA.** FK'lar (P-34) ancak benzersizlik kısıtları, şube filtresi, yeni hareket aileleri **ve abonelik tabloları (P-27)** oturduktan sonra konur.

12. **MEVCUT VARLIK YENİDEN YAZILMAZ.** Bugün `test -e` ile doğrulandı — hepsi **MEVCUT**: `Bilesenler/SatirIciDuzenle.razor`, `dotnet/yetim-tara.sh`, `dotnet/kurulum.sh`, `kurulum.ps1`, `yayin-al.sh`, `yedek-al-ve-dogrula.sh`, `yedek-ortak.sh`, `docs/RUNBOOK-PILOT-DAVET.md`, `Pages/Kasalar.razor`, `Pages/UrunAgaci.razor`, `Modules/Urun/BelgeKapisi.cs`, `ham/surum.json`. Her paket başlamadan `dokunulan_dosyalar` listesi `test -e` ile doğrulanır.

13. **KURUCU KALEMİ İŞİ DURDURMAZ, SINIRI ÇİZER.** Dış bağımlı kalemler ayrı bölümde bekler; ekip onların yerine geçici çözüm uydurmaz, **"bu kurulumda hariç"** beyanını ürün içinde görünür yazar. Kurucuya açık sorulmuş bir karar (ör. KMH negatif açılış) plan tarafından tek taraflı mühürlenmez.

14. **BÜTÇE DE ÖLÇÜLÜR.** Dalga toplamı = (paket dakikaları toplamı × 1,4) + (QA turu sayısı × ölçülen tur süresi). Formül plan metnindedir; her dalga sonunda "tahmin / gerçekleşen / sapma katsayısı" satırı `ORKESTRATOR-DURUM.md`ye yazılır (Mustafa'nın K-F1/K-F6 koşulu).

---

## § 2 — DALGALAR (36 paket, 9 dalga)

> Her paket: **kapsam kimlikleri · önkoşul · dosyalar · migration · DoD · sayaçlar · tahmin · risk · sürüm**.
> Dalga sonu kapısı sabittir: `kapi.sh` → Release yayını → QA turu → düzeltme → temiz tur → **değişiklik yapılmadan ikinci doğrulama turu** → hazırlık kurulu → sayaç birleştirme.

---

### DALGA 0 — İzlenebilirlik ve karar zemini (kod dokunulmaz)

**Amaç:** planın "hiçbiri unutulmadı" iddiasını denetlenebilir kılmak ve karar bekleyen sürümleri mühürlemek.
**Ajan-saat:** 3,9 (QA turu yok) · **Kurucu gözetim:** ≈0,5 gün

#### P-00 — KAPSAM ENVANTERİ + KÖKEN TABLOSU + TRİYAJ KURALI
- **Kimlikler:** B-50, B-51, B-58, Y-42, R-16
- **Önkoşul:** yok — planın ilk adımı, **mühürden önce biter**
- **Dosyalar:** `dokumanlar/09-uygulama-kayitlari/KAPSAM-ENVANTERI.md` (YENİ) · okuma: `QA-TARAYICI-TURU-2026-09-16-tur4.md`, `...-tur5.md`, `PAKET-S-3e-uygulama-raporu.md`, `PAKET-S-3f-uygulama-raporu.md`, `dokumanlar/02-mimari/MIMARI-KALAN-PAKETLER-2026-09-15.md`
- **Migration:** HAYIR
- **DoD:**
  - Her kimlik → kaynak (TB-xx, QA turu satırı, rapor §) satır satır eşlenir; kaynağı bulunamayan kimlik **"bu oturumda sentezlendi"** diye açıkça işaretlenir
  - Tur 4 (148 devralınan) + tur 5 (193 bulgu, 119 AÇIK, ≈59 yeni) satır satır açılır; her bulgu ya mevcut kimliğe eşlenir ya yeni kimlik alır — eşleme tablosu belgeye girer
  - S-3e § 12 ve S-3f § 11-12 açık kalanları tek tek listelenir; K-23…K-26 ve B-55/B-56 buradan doğar
  - Y-42 triyaj kuralı yazılır (24 saat, dört kutu, `surum.json` damgası)
  - **Kapsam freni (Ramazan K1):** dalga başına yeni kimlik sayısı ölçülür ve `ORKESTRATOR-DURUM.md`ye tek satır yazılır
- **Sayaçlar:** `KIMLIK_KOKENSIZ = 0` · `BULGU_DAMGASIZ = 0` · `YENI_KIMLIK_DALGA` (raporlanır)
- **Tahmin:** 120 ajan-dk · **Risk:** kimlik sayısı 158'in de üstüne çıkabilir — gerileme değil, dürüst sayım. Eşleme tablosu çıkmadan hiçbir dalga başlamaz (kapı).
- **Sürüm:** 1.0

#### P-01 — SÜRÜM MÜHRÜ + DALGA 2b DEVİR KAYDI
- **Kimlikler:** R-9, R-11, R-12, R-13, R-14, R-15, R-19
- **Önkoşul:** P-00
- **Dosyalar:** `ham/surum.json` (ÖLÇÜLDÜ: 6.453 bayt, 16 Eylül — mevcut) · `betikler/v1-surum-muhru.js` · `betikler/surum-kaydet.js` · `ORKESTRATOR-DURUM.md` · `dotnet/denetim.sh` (kendi işaretli sayaç bloğu — **Bertan K8 gereği denetim.sh seri sırası P-01 ile başlar**)
- **Migration:** HAYIR
- **DoD:**
  - `ham/surum.json`'da "karar bekliyor" = 0; her paket {1.0, 1.1, KAPSAM-DIŞI} kümesinden birine yazılı
  - **R-15 sıra 0 mührü:** sadeleştirme (Y-5 / Y-9 / Y-29) yeni modülden önce gelir
  - **R-13 oyu 2-2:** kurul oylamasıyla kırılır; kırılmazsa Tamer Bey mühürler — kurucuya çıkılmaz
  - Dalga 2b'nin 7 paketi (UX-4d, UX-1-B, 38-A, 48-A, 45-API, 6-A, 3-A) tek tek **bitti / yarım / terk** olarak yazılır; yarım olan hangi P-xx'e devrettiği belirtilir
  - Mevcut mühür mekanizması (`v1-surum-muhru.js` + ana belge § MÜHÜR 2-4) ile `ham/surum.json` ilişkisi yazılır; **tek kaynak ilan edilir**, ikinci kaynak bırakılmaz
  - **Kurucu kalemleri ihtiyaç tarihi tablosu** (Ramazan K4) bu paketle birlikte kurucuya iletilir
- **Sayaçlar:** `SURUM_KARAR_BEKLEYEN = 0` (sabit cırcır)
- **Tahmin:** 45 ajan-dk · **Risk:** R-13 kırılmazsa D-11 ve P-18 sıraya giremez.
- **Sürüm:** 1.0

---

### DALGA 1 — Mekanizma ve veri doğruluğu zemini

**Amaç:** ölçümü kanıt hâline getirmek, sözleşme tavanını açmak, şube izolasyonunu ve benzersizliği DbContext düzeyinde kapatmak. **Bu dalga bitmeden hiçbir ekran paketi başlamaz.**
**Ajan-saat:** 36,3 (2 QA turu dâhil) · **Kurucu gözetim:** ≈1 gün

#### P-02 — YAYIN DAMGASI + ÖLÇÜM KAPISI + TÜM SAYAÇ TABANLARININ TAZE ÖLÇÜMÜ
- **Kimlikler:** B-1, B-2, B-21, B-53, Y-33
- **Önkoşul:** P-01
- **Dosyalar:** `dotnet/denetim.sh` · `dotnet/yayin-al.sh` · `betikler/kapi.sh` · `betikler/qa-turu.js` · `betikler/hazirlik-kurulu.js` · `betikler/damgala.js` · `.claude/launch.json` · `dotnet/mut.sh` (B-53: `/private/tmp`'den kopyalanır, **silme kurucuya kalır**)
- **Migration:** HAYIR
- **DoD:**
  - Yayın damgası = ağaç damgası; uyuşmazlıkta **5199 açılmaz** (fail-closed). `ASPNETCORE_ENVIRONMENT=Production` sabitlenir, künyede "Ortam: Üretim" **canlı kanıtı** rapora girer
  - 42/42 rota taranır (bugün ölçüldü: 42 .razor, 42 `^@page`, alt dizin yok). Öykünme kapalı, gerçek giriş
  - **Tüm cırcır tabanları taze derlemede ölçülür, belgeden kopyalanmaz.** Her sayacın **ölçüm komutu** `denetim.sh`'ta yorum olarak yazılı olur (Bertan K7); tabanlar `ham/olcum-<tarih>.json` olarak sürümlenir
  - **`MUTORJ_ARTIK` sayacı kurulur** ve tüm grepler `--include='*.razor'` kullanır (bugünkü yeni bulgu: Pages'te 5 `.mutorj` artığı grep'i 42 yerine 47 gösteriyor)
  - `BEKLENEN_VARLIK` (bugün 51) gerçek `IKiraciVarlik` sayısına **-eq** ile sabitlenir; B-21'in "37" iddiası **doğrulanır, kopyalanmaz**
  - `SAYFA_RECORD` kapsamı `Pages(42) + Bilesenler(27) + Layout(5)` olarak genişletilir
  - **Bir tam QA turu kronometreyle ölçülür** ve "≈8 ajan-saat" varsayımı `ham/qa-tur-suresi.json`'a gerçek değerle yazılır (Mustafa K-F2); sonraki tüm dalga bütçeleri bu değerle yeniden hesaplanır
  - **`ICLOUD_ESITLEME_ACIK` kontrolü** eklenir; açıkken kapı UYARI verir ve o dalganın süre ölçümleri "geçersiz taban" damgası alır (Mustafa K-F5)
  - `kapi.sh`'a **`TEK_EL_IHLAL = 0`** sayacı eklenir (kilit dosyası + P-xx damgası) ve bir kez bilerek kırılıp FAIL verdiği kanıtlanır (Bertan K5)
- **Sayaçlar:** `YAYIN_TAZE` · `ROTA_KAPSAM` (-eq 42) · `BEKLENEN_VARLIK` (-eq) · `SAYFA_RECORD_TAVAN` (azalan, geniş kapsam) · `MUTORJ_ARTIK` · `TEK_EL_IHLAL = 0` · `ICLOUD_ESITLEME_ACIK`
- **Tahmin:** 120 ajan-dk · **Risk:** Production'a sabitlenince geliştirme kısayolları kapanır, QA turu 6'da yeni "boş ekran" bulguları çıkar — gerileme değil ilk gerçek ölçüm. K-20 mtime'ları oynatırsa `YAYIN_TAZE` sahte "bayat" verir; K-20 kapanana dek uyarı modunda.
- **Sürüm:** 1.0

#### P-03 — SÖZLEŞME-1: Kuyumcu.Contracts + tip/ad sözleşmesi
- **Kimlikler:** R-10
- **Önkoşul:** P-02
- **Dosyalar:** `dotnet/Kuyumcu.sln` · `dotnet/Kuyumcu.Contracts/` (YENİ proje) · `dotnet/Kuyumcu.Core/Sozlesmeler/` (8 dosya) · `Kuyumcu.Web/Pages/*.razor` (**42 dosya — TEK EL**) · `Kuyumcu.Web/Bilesenler/*.razor` (27) · `Kuyumcu.Tests/` (yansıma testleri) · `dotnet/denetim.sh` (kendi bloğu)
- **Migration:** HAYIR
- **DoD:**
  - Taşıma **saf yer değiştirmedir**: alan eklenmez, davranış değişmez
  - Her taşınan tip için sunucu DTO ⇄ istemci tip **ad + TİP** eşleşmesi yansımayla testte kilitlenir (çalışma zamanı NRE'si derlemede görünmez — QA turu 2 dersi)
  - Tavan tek adımda **129 → 100**, dalga sonunda **→ 60**; artırma yasak
  - Yeni istemci record açılmaz; yeni uç önce Contracts tipini alır
- **Sayaçlar:** `SAYFA_RECORD_TAVAN` (azalan cırcır) · `CONTRACTS_DISI_MODEL = 0`
- **Tahmin:** 240 ajan-dk · **Risk:** 42 razor **tek eldir** — bu paket koşarken hiçbir ekran paketi paralel koşamaz. Yanlış ad eşlemesi derleme hatası vermez, çalışma zamanında patlar.
- **Sürüm:** 1.0

#### P-04 — ŞUBE BAĞLAMI + GLOBAL SORGU FİLTRESİ
- **Kimlikler:** Y-1
- **Önkoşul:** P-03
- **Dosyalar:** `Data/KuyumcuDbContext.cs:169-172` (tek jenerik TenantId filtresi), `:569` (Tenant ayrı filtresi) · `Data/KiraciBaglami.cs` (**bugün şube kavramı YOK**: yalnız KiraciId + SistemModu) · `Auth/AktifKullanici.cs` (SubeId NULL'lanabilir) · `Data/Entities.cs` · `Modules/**/*.cs` (**ÖLÇÜLDÜ: `SubeId ==` 63 nokta**) · `Kuyumcu.Tests/`
- **Migration:** ŞARTLI — şema değişmez; yalnız `sube_id`'si boş/yetim geçmiş satır varsa geri doldurma. Önce salt okunur ölçüm raporu; `Down()` zorunlu; `--no-build` YASAK
- **DoD:**
  - **Önce 63 noktanın sınıflandırma tablosu yayımlanır** (kod değişimi tablo mühürlenmeden başlamaz — Bertan K2, Mehmet koşulu):
    (a) kimlikten gelen SubeId ile **yetki** amaçlı süzgeç → kaldırılır
    (b) parametreyle gelen **kullanıcı seçimli** rapor süzgeci (`RaporModulu.cs:753,861,930`; `SatisRaporlari.cs:50,199,256`) → **dokunulmaz**
    (c) iç eşleştirme/join (`RaporModulu.cs:960`) → dokunulmaz
    (d) `Yerlesim/YerlesimServisi.cs` 11 noktası → sınıflandırılır
  - **Şube bağlamı bu pakette çözülür** (hasım bulgusu: taslakta 3 dalga sonraya kalmıştı): `IKiraciBaglami`'ye şube + **NULL davranışının yazılı sözleşmesi** + "TÜM şubeler" rolü. `SubeId = null` jetonda beklenen davranış testle kilitlenir — **fail-open yasak**
  - EF tek filtre sınırı: TenantId ve SubeId **tek ifadede AND** ile birleşir; Tenant'ın `:569`'daki ayrı filtresi istisna olarak gerekçelendirilir
  - `IgnoreQueryFilters` çağrı noktaları (**bugün ölçüldü: 4**) tek tek gerekçelenir ve beyaz listeye yazılır; sayı artamaz
  - **Negatif test:** A şubesi jetonuyla B şubesi kaydı → 404, **QA tarafından gerçek HTTP isteğiyle canlı doğrulanır** (Aşkın koşulu). Filtre açılınca **kaç mevcut test kırıldı ölçülür**; susturulan test = 0
- **Sayaçlar:** `GUVENLIK_ELLE_IZOLASYON = 0` (azalan; taban sınıflandırma tablosundan) · `KULLANICI_SUZGEC` (yalnız raporlanır, azaltma hedefi YOK) · `IGNORE_QUERY_FILTERS ≤ 4` (-le)
- **Tahmin:** 360 ajan-dk · **Risk: YÜKSEK.** Filtre yanlış kurulursa veri ya sızar ya görünmez olur. Kör kaldırma "şubeye göre rapor al" özelliğini siler — sınıflandırma tablosu atlanamaz. Şube indeksi yoksa yük testi (P-25) burada patlar; indeksler bu pakette eklenir.
- **Sürüm:** 1.0

#### P-05 — BENZERSİZLİK KISITLARI + ÇİFT KAYIT ÖLÇÜMÜ
- **Kimlikler:** Y-2
- **Önkoşul:** P-04
- **Dosyalar:** `Data/KuyumcuDbContext.cs` (**135 HasIndex bloğu**) · `Data/Entities.cs` · `Data/Migrations/` (yeni) · `Modules/Urun/UrunModulu.cs` · `Modules/Cari/CariModulu.cs` · `Kuyumcu.Tests/`
- **Migration:** EVET — `stoklar(tenant, sube, urun)`, `urunler(tenant, kod)`, `urunler(tenant, barkod)`, `cariler(tenant, vergi_no)` filtreli/NULL toleranslı; `CariIban(tenant, iban_hash)` zaten var → yalnız doğrulanır. Her indeks için `Down()`
- **DoD:**
  - Migration'dan **önce** her kısıt için salt okunur çakışma raporu; çakışma varsa migration **koşmaz**, birleştirme yolu kurucuya rapor edilir (K-1 ailesi)
  - Eşzamanlı iki isteğin aynı kod/barkod/vergi_no'yu yazması testte **409** döner; ikinci kayıt sessizce oluşmaz
  - 409 Türkçe **alan altı** mesajıyla döner (DogrulamaSozlugu)
  - MySQL'de NULL çoklu kabulü **doğrulanmalı**; vergi_no boş kayıtlar yaygınsa alan NULL'lanabilir tutulur
  - `Down()` gerçekten bir kez koşturulur, çıktısı rapora eklenir (Aşkın koşulu)
- **Sayaçlar:** `DOGAL_ANAHTAR_KISITSIZ = 0` (azalan, beyaz liste gerekçeli)
- **Tahmin:** 150 ajan-dk · **Risk:** canlıda çakışan satır varsa migration ortada durur — geri dönüş betiği DoD'a girmeden başlanmaz.
- **Sürüm:** 1.0

**Dalga 1 kapısı:** `kapi.sh` → Release yayını → QA turu → düzeltme → temiz tur → **değişiklik yapılmadan ikinci doğrulama turu** → hazırlık kurulu. **Kabul:** kritik = 0 VE düzenlenemeyen = 0, **iki ardışık turda**.

---

### DALGA 2 — Mekanizma II: düzenlenebilirlik, dayanıklılık, defter sözleşmesi, erişim

**Amaç:** her yeni ekranda yeniden üretilen mekanizma borcunu şablon/bileşen/sayaç düzeyinde kapatmak; Mehmet'in MFA vetosunu kaldırmak.
**Ajan-saat:** 37,7 · **Kurucu gözetim:** ≈1 gün

#### P-06 — DÜZENLENEBİLİRLİK ŞABLONU (mevcut SatirIciDuzenle GENİŞLETİLİR)
- **Kimlikler:** B-4, B-13, B-18, B-34, B-54, Y-15
- **Önkoşul:** P-03
- **Dosyalar:** `Bilesenler/SatirIciDuzenle.razor` (**ÖLÇÜLDÜ: MEVCUT** — S-3c'de mühürlenen ortak sözleşme; YENİ DEĞİL, genişletilir) · `Bilesenler/DegismezIpucu.razor` · `Bilesenler/DuzenlenebilirListe.razor` (YENİ) · `Kuyumcu.Core/I18n/DogrulamaSozlugu.cs` · `Modules/Meta/DuzeltmePolitikasi.cs:87` · `Kuyumcu.Tests/UX1C3DuzeltmeSozlesmeTestleri.cs`
- **Migration:** HAYIR
- **DoD:**
  - **Mevcut sözleşme davranışı (Enter/Esc/blur) korunur; gerileme testi eklenir** (hasım bulgusu R10)
  - Enter tam 1 istek üretir, Esc vazgeçer, açılışta ilk alan odaklanır (`document.activeElement` kanıtı)
  - Dolu formda **ilk** Kaydet istek üretir (B-34 blur/binding yarışı, mutasyonla kanıtlanır)
  - PATCH sözleşmesi: gönderilmeyen alan = değişmedi, testle kilitlenir
  - Her doğrulama hatası **alan altında**; "işaretli alanları düzeltin" tek başına yasak
  - B-54'ün 21 gerekçeli istisnası ekranda **gerekçe + somut düzeltme yolu** ile yazılır ve paydadan düşülür; gizleyerek sayaç düşürmek yasak
  - bUnit tek başına yeterli değil: odak/Enter kanıtı her dalgada QA turunda **gerçek tarayıcıyla** çapraz doğrulanır
- **Sayaçlar:** `DUZENLENEMEYEN_GIRIS` (azalan; **taban P-02'de ölçülen değerdir** — tur 5 belgesi 32, kurul 4 "13-49 arası" diyor; **"49" plandan çıkarıldı**) · `DUZENLEME_KALIBI_YOK_SAYFA = 0` · `ALAN_ANAHTARSIZ_DOGRULAMA = 0` · `BU_PAKETTE_EKLENEN_RECORD = 0`
- **Tahmin:** 300 ajan-dk · **Risk:** şablon çok katı olursa gerekçeli istisnalar yanlış yere düşer; istisna listesi Aşkın onayından geçer.
- **Sürüm:** 1.0

#### P-07 — API DAYANIKLILIK BANDI: 429, geri çekilme, jeton yarışı, istek fırtınası
- **Kimlikler:** B-10, B-57
- **Önkoşul:** P-03, P-06 (BasitSatisAkisi.razor bir bileşendir)
- **Dosyalar:** `Kuyumcu.Web/Servisler/ApiIstemci.cs` · `Servisler/OturumServisi.cs` (**HttpClient tüketicisi — jeton yarışı burada**) · `Servisler/ArayuzModuServisi.cs` (**HttpClient tüketicisi**) · `Bilesenler/BasitSatisAkisi.razor` (**HttpClient tüketicisi**) · `Kuyumcu.Web/Program.cs` · `Kuyumcu.Api/Program.cs` (AddRateLimiter) · `Kuyumcu.Tests/`
- **Migration:** HAYIR
- **DoD:**
  - 429'da ekran sessizce boşalmaz: "Yeniden dene" + kalan süre
  - Üstel geri çekilme + jitter, en fazla N deneme
  - Jeton yenileme yarışı **tek bekleyici** deseniyle çözülür ve eşzamanlı 5 istekle **canlı** kanıtlanır
  - **B-57:** tek sayfa ömründe aynı uca N'den fazla istek testi kırar (tur 5 kanıtı: `kur/guncel` 130+ tekrar, arada bir 401)
- **Sayaçlar:** `APIISTEMCI_DISI_HTTPCLIENT = 0` (bugün ölçülen 4 tüketici; `Program.cs` kaydı beyaz listede) · `ISTEK_TEKRAR_TAVANI` (azalan)
- **Tahmin:** 150 ajan-dk · **Risk:** oran sınırı QA koşumunu yavaşlatır; QA jetonu beyaz listeye alınır.
- **Sürüm:** 1.0

#### P-08 — HAREKET TÜRÜ SÖZLEŞMESİ + ÇEKİRDEK KOLONLAR + FATURA BAĞI
- **Kimlikler:** Y-32, Y-14, Y-13, Y-25, B-52
- **Önkoşul:** P-05
- **Dosyalar:** `Data/Entities.cs:28-59` (HareketTurleri), `:493-694` (Hareket) · `Data/HareketFabrikasi.cs` · `Data/DenetimIzi.cs` · `Data/FaturaKalemi.cs:34` (**hareket_id — bağ ZATEN VAR**) · `Modules/Fatura/FaturaModulu.cs:658` · `Modules/Rapor/SatisRaporlari.cs:24` · `Data/Migrations/` · `Kuyumcu.Tests/`
- **Migration:** EVET — `hareketler.adet decimal(12,3) NULL`, `hareketler.personel_id varchar(36) NULL` + `(tenant, personel_id, tarih)` indeksi. **`fatura_id` kolonu AÇILMAZ.** `Down()` zorunlu
- **DoD:**
  - **Y-25 YENİDEN YAZILDI** (hasım bulgusu, doğrulama verdicti YANLIS): kimlik düzeyinde bağ **zaten var** (`FaturaKalemi.cs:34`, `FaturaModulu.cs:658`, `Entities.cs:851`). İş yeni kolon açmak **değil**, `fatura_kalemleri.hareket_id` üzerinden okuyan **tek sorgu yolu** + `fatura_no` serbest metninin emekliye ayrılmasıdır
  - **İlişkinin yönü (1 fatura ↔ N hareket) ADR olarak yazılmadan** hiçbir bağ migration'ı açılmaz (Bertan K4 — `dokumanlar/02-mimari/ADR-00xx`); iki paralel bağ oluşursa mutabakat (P-24) ikisini birden saymak zorunda kalır
  - `HareketTurleri` kapalı listesi `HareketFabrikasi`'nda **kapı** olur; listede olmayan Tur 400 döner
  - Geçmiş satırlarda `adet` NULL kalır; rapor "bilinmiyor" yazar, **varsayım üretmez**
  - `personel_id` **"SATAN"** olarak tanımlanır ve kilitlenir; prim **hesabı** 1.1'de (D-2)
- **Sayaçlar:** `HARETURU_DISI_SABIT = 0` · `HAREKETFABRIKASI_DISI_YAZMA = 0` · `FATURA_NO_SERBEST_METIN` okuma yolu = 0 (azalan)
- **Tahmin:** 180 ajan-dk · **Risk:** tür kapısı sıkılaşınca eski bir modülün yazdığı gayriresmî tür kırılabilir — bulgu, gizlenmez.
- **Sürüm:** 1.0

#### P-09 — KİMLİK, ERİŞİM, MFA VE YETKİ MATRİSİ
- **Kimlikler:** B-6, B-20, B-35, B-36, B-37, Y-39 · **+ Ramazan K3 dilimi:** yetkisiz `PlanGuncelle` ucu
- **Önkoşul:** P-04
- **Dosyalar:** `Modules/Auth/MfaModulu.cs:110-134,340-384` · `Modules/Auth/AuthModulu.cs:244,431` · `Modules/Kullanici/KullaniciModulu.cs` · `Modules/Yetki/YetkiModulu.cs:34,63` · `Modules/Admin/AdminModulu.cs:161-169` (**K3 dilimi**) · `Layout/RotaBasliklari.cs` · `Pages/Kullanicilar.razor`, `Yetkiler.razor`, `Ayarlar.razor` · `Kuyumcu.Tests/`
  > **Sıra kuralı:** `Modules/Auth/` tek pakettir ve **seri** işlenir — önce MFA sertleştirme, sonra 401 hizalaması.
- **Migration:** ŞARTLI — SAHİP devri için denetim olayı yeterli olabilir (**doğrulanmalı**); yetki matrisi otomatik üretimi geçiş migration'ı gerektirebilir
- **DoD:**
  - MFA **uçtan uca canlı** ölçülür (kurulum → giriş → kurtarma → kapatma) tek kullanımlık kiracıda; kiracı sonunda silinir, `QA-TEST-*` fikstürlerine dokunulmaz
  - Kurtarma kodu **yalnız başarılı doğrulamada** tüketilir (**mut.sh mutasyon kanıtı** — Aşkın koşulu: bUnit tek başına yeterli değil); `mfa-kurtarma-kullanildi` denetim olayı **DB'de satır olarak** doğrulanır; uç adları hizalanır
  - Yarım kalan MFA kurulumu `/ayarlar`'da kilit yaratmaz, sıfırlanabilir
  - Başarısız girişte **kullanıcı varlığı ifşa edilmez**: `AuthModulu.cs:244` ve `:431` aynı generic **401** döner (bugün 404 + "Kullanıcı bulunamadı")
  - "Yeni SAHİP ata ve devret" akışı: son SAHİP kendini kilitleyemez; kullanıcı pasife alınabilir (**hard DELETE yok**)
  - Yetki matrisi `RotaBasliklari`'ndan **otomatik** üretilir; Emanet, Kur, Vezne, POS, Sayım, Uyum, Yedekler, Veri aktarımı, Denetim İzi, Fiyat Listeleri eklenir. **Negatif test:** özniteliksiz uç eklenince sayaç **kırılır**
  - **K3 dilimi (Ramazan):** `AdminModulu.cs:161-169` yetkisiz `PlanGuncelle` bir **güvenlik açığıdır**, ticari özellik değil — yetki kapısı + denetim izi bu dalgada kapanır; abonelik tablolarının kalanı DALGA 7'de
  - **Mehmet'in vetosu bu paketin canlı kanıtı olmadan kalkmaz**
- **Sayaçlar:** `ROTA_YETKI_ESLESMEZ = 0` · `OZNITELIKSIZ_UC = 0` · `TEK_SAHIP_KILIT_YOLU = 0` · `YETKISIZ_PLAN_YAZAN_UC = 0`
- **Tahmin:** 300 ajan-dk · **Risk:** otomatik türetim yanlışsa tüm ekranlar herkese açılır (fail-open) → negatif test zorunlu. `[RolGerekli]` özniteliğinin **tanım dosyası hasım turunda bulunamadı** — paket başlamadan aranır (doğrulanmalı).
- **Sürüm:** 1.0

**Dalga 2 kapısı:** standart kapı + **sayaç birleştirme adımı** — her paketin `denetim.sh`'a eklediği `# >>> P-xx` / `# <<< P-xx` işaretli blokları tek elden birleştirilir (hasım bulgusu R5: "tek el" kuralı 13 paketin sayacını yazılamaz kılıyordu).

---

### DALGA 3 — Yazılmış ama bağlanmamış ucuz kablolar

**Amaç:** motoru/kolonu hazır olup ekrana bağlanmamış işleri düşük maliyetle kapatmak; dış kaynak hatalarında veri kaybı hissini bitirmek.
**Ajan-saat:** 38,4 · **Kurucu gözetim:** ≈1 gün

#### P-10 — SUNUCUNUN KABUL ETTİĞİ ALANLARI FORMA BAĞLA + ÜRÜN GÖRSELİ
- **Kimlikler:** Y-3, Y-8
- **Önkoşul:** P-06
- **Dosyalar:** `Modules/Urun/UrunModulu.cs:124,127,128` · `Modules/Satis/OdemeSepeti.cs:52,55` · `Modules/Urun/BelgeKapisi.cs:41-42` (**ÖLÇÜLDÜ: `Modules/Belge` DEĞİL, `Modules/Urun` altında**) · `Pages/Urunler.razor` · `Bilesenler/` (görsel galerisi) · `Kuyumcu.Tests/`
- **Migration:** HAYIR
- **DoD:** 7 alan (RFID, KMTS, görsel URL, açıklama, POS cihaz/taksit, parite, notlar) forma bağlanır · ürün görseli/galerisi kartta gösterilir (Gorsel/Fotograf türleri) · DTO ⇄ form eşleşme sayacı devreye alınır
- **Sayaçlar:** `DTO_FORM_ESLESMEZ = 0` (beyaz liste gerekçeli) · `BU_PAKETTE_EKLENEN_RECORD = 0`
- **Tahmin:** 120 ajan-dk · **Risk:** RFID alanı donanımı ima eder; ekranda **"donanım kapsam dışı (K-21)"** beyanı yazılır.
- **Sürüm:** 1.0

#### P-11 — DIŞ KAYNAK DAYANIKLILIĞI: KUR + BİLDİRİM
- **Kimlikler:** B-14, B-15, B-16, B-22, B-23, B-24
- **Önkoşul:** P-06, P-07
- **Dosyalar:** `Modules/Kur/KurModulu.cs` · `Pages/Kur.razor` · `Pages/Bildirimler.razor` · `Data/ZamanlanmisIs.cs` · `Kuyumcu.Tests/S3eKurKaynagiEkranTestleri.cs`
- **Migration:** HAYIR
- **DoD:**
  - Kaynak kapalıyken "Şimdi çek" **pasif**; 503 sonrası mevcut kur kartı **DOM'da kalır** (hata sınırı)
  - Kur geçmişinde "İptal et" gerçekten istek üretir ve sonucu gösterir; kayıt düzenlenebilir
  - **B-16 kalıntısı** (MANUAL ₺6.820,50) temizlenir ya da "geçersiz kılındı" işaretlenir; sessizce bırakılmaz. **Her dalga raporunda "geri alınamayan değişiklik" bölümü tutulur**
  - `/bildirimler` basit mod menüsünde görünür; zamanlanmış iş ayarları (saat/eşik/açık-kapalı) düzenlenebilir; 3 satır içi `style` kalkar
  - Zamanlayıcı döngüsünün canlı kanıtı **üçüncü kez** denenir (TurDakika=1, 5198); üretilemezse **KAPANMADI yazılır ve eskale edilir** (Anayasa md.1)
- **Sayaçlar:** `DIS_HATADA_VERI_DUSUREN_YOL = 0` · `SATIR_ICI_STYLE` (azalan)
- **Tahmin:** 240 ajan-dk · **Risk:** zamanlayıcı kanıtı iki turdur üretilemedi; üçüncüde de üretilemezse dalga kapanmaz.
- **Sürüm:** 1.0

#### P-12 — DEFTER TARAFI DÜZENLENEBİLİRLİK + STORNO GÖRÜNÜRLÜĞÜ
- **Kimlikler:** B-41, B-42, B-43, B-38, B-19
- **Önkoşul:** P-06, P-08
- **Dosyalar:** `Pages/Kasalar.razor` (**ÖLÇÜLDÜ: gerçek ad Kasalar.razor**) · `Pages/Banka.razor`, `Emanet.razor`, `Vezne.razor` · `Modules/Kasa/KasaModulu.cs`, `Banka/BankaModulu.cs`, `Emanet/EmanetModulu.cs` · `Modules/Satis/StornoServisi.cs`, `StornoSuzgeci.cs` · `Kuyumcu.Tests/`
- **Migration:** HAYIR
- **DoD:**
  - Finansal alanlar (tutar, has gram, ayar) **değil**; açıklama / karşı taraf / tarih düzenlenebilir olur. Finansal düzeltme **yalnız storno + yeni kayıt**
  - Vezne "Düzenle" penceresine **İşçilik (TL)** alanı eklenir
  - Storno her ilgili ekranda görünür; 48-A Faz B parti geri alma yolu ekrandan erişilir
  - Her kilitli alanın yanında **gerekçe + somut düzeltme yolu**; `DenetimIzi` her değişikliği eski/yeni değerle yazar
- **Sayaçlar:** `STORNO_DISI_FINANSAL_PATCH = 0` (zorunlu) · `STORNO_GORUNMEYEN_EKRAN = 0`
- **Tahmin:** 240 ajan-dk · **Risk:** açıklama düzenlenebilir olunca denetim izi büyür; `SaklamaSuresiIsi` ile çakışması kontrol edilir.
- **Sürüm:** 1.0

#### P-13 — KART / AYAR / UYUM EKRAN BORÇLARI
- **Kimlikler:** B-39, B-40, B-47, B-48, B-27, B-49, B-12, B-17
- **Önkoşul:** P-06
- **Dosyalar:** `Pages/Cariler.razor`, `UrunAgaci.razor` (**ÖLÇÜLDÜ: `UrunAgac.razor` DEĞİL**), `Isletme.razor`, `Yedekler.razor`, `Uyum.razor`, `GiderPusulalari.razor`, `Kurulum.razor` · `Modules/Cari/CariModulu.cs`, `Urun/UrunModulu.cs`, `Masak/MasakModulu.cs:314-326` · `Data/Kurulum.cs` · `Kuyumcu.Tests/`
- **Migration:** ŞARTLI — Aktif/pasif kolonu Urun/Cari/Kategori'de var mı **önce ölçülür**; yoksa EVET
- **DoD:**
  - **Hard DELETE yok:** pasife alma + bağlı kayıt sayısı uyarısı (aktif cari, kategori)
  - Kur kaynağı ve maliyet yöntemi boşken kilitli kalmaz; `/yedekler` iptal gerekçesi ekranda okunur
  - `/uyum` GONDERILDI kaydında **düzeltme yolu tanımlanır** (storno/ek beyan); kimlik tespiti kaydı üretilip ölçülür
  - Gider pusulası satıcı kimliği alanları MASAK maskeleme desenini (`MasakModulu.cs:314-326`) kullanır; adres boş bırakılamaz (VUK md.234); `/isletme` telefon doğrulaması şube formuyla hizalanır
  - **B-17 — K-24 (KMH) kararı gelene dek:** negatif açılış için **mevcut tutucu davranış korunur** ve ekranda gerekçe yazılır. **Plan tek taraflı "negatif açılış reddedilir" hükmü KOYMAZ** (hasım bulgusu, taslak kararı geri çekildi)
  - **K-25 gelmezse:** ekran kullanıcıyı geçmiş boş adresli kayıtları doldurmaya **zorlamaz**
  - Ham hata kodu (`DOVIZ_HESAP_FAZ_B`) kullanıcıya gösterilmez; Türkçe mesaj + hata kimliği. Mühür geri alınamıyorsa mühürden **önce** açık uyarı
- **Sayaçlar:** `GEREKCESIZ_KILITLI_ALAN = 0` · `IC_KOD_SIZINTISI = 0` (PAKET n / FAZ_X / büyük harf iç kod desenleri)
- **Tahmin:** 360 ajan-dk · **Risk:** `/uyum`'da düzeltme yolu olmaması hukuki sorumluluk taşır. K-24 gecikirse yalnız B-17 dilimi bekler, paket durmaz.
- **Sürüm:** 1.0

**Dalga 3 kapısı:** standart kapı + sayaç birleştirme. **Ek (Ramazan K5 / Eray):** pilot davet metni ve kısıt beyanı bu kapının kontrol listesinde **hazır** olur; gönderim kurucudadır (K-19).

---

### DALGA 4 — Tezgâh katmanı: menü, arama, satış fişi, yazdırma, mobil

**Amaç:** R-15 sıra 0 mührü — **sadeleştirme yeni modülden önce gelir.** Kuyumcunun günlük kullandığı yüzey burada kapanır.
**Ajan-saat:** 55,2 · **Kurucu gözetim:** ≈1 gün

#### P-14 — MENÜ / BİLGİ MİMARİSİ + ETİKET ÇARPIŞMALARI
- **Kimlikler:** Y-9
- **Önkoşul:** P-01 (R-15 mührü)
- **Dosyalar:** `Layout/KenarCubugu.razor` (**ÖLÇÜLDÜ: `Layout` altında, `Paylasilan` değil; 38 href, 0 grup başlığı**) · `Layout/RotaBasliklari.cs` · `Kuyumcu.Core/I18n/Sozlukler.cs:2770,2777` · `Kuyumcu.Tests/`
- **Migration:** HAYIR
- **DoD:** 38 bağlantı gruplanır, grup başlığı > 0 · 4 etiket çarpışması çözülür, aynı metin iki farklı hedefe gitmez · **basit mod tutarlılığı:** rozeti olan her özelliğin sayfası basit mod menüsünde de var
- **Sayaçlar:** `MENU_AYNI_AD = 0` · `MENU_KAPSAM` (erişilemeyen `@page` = 0) · `BASIT_MOD_TUTARSIZ = 0`
- **Tahmin:** 120 ajan-dk · **Risk:** menü değişimi kılavuzu (P-33) ve yetki matrisini bayatlatır — ikisi de bundan **sonra**.
- **Sürüm:** 1.0

#### P-15 — KABUK: GENEL ARAMA, KOMUT PALETİ, ŞUBE SEÇİCİ, KAYIT SEÇİCİ, DERİN BAĞLANTI
- **Kimlikler:** Y-29, Y-5, B-25, B-29
- **Önkoşul:** P-14, P-04
- **Dosyalar:** `Bilesenler/KayitSecici.razor` (YENİ), `GlobalArama.razor` (YENİ), `SubeSecici.razor` (YENİ) · `Layout/AnaYerlesim.razor`, `UstBar.razor` · `Modules/Arama/` (YENİ uç) · `Pages/Faturalar.razor` (**yalnız `#ff-fis` alanı**) · `Kuyumcu.Tests/`
- **Migration:** HAYIR
- **DoD:**
  - `limit=200` düz açılır listeler kalkar; seçici **sunucuda arar ve sayfalar**
  - "Fişten Fatura Kes" 26 haneli ULID yerine **ad + tip** gösteren kayıt seçici kullanır; 1-A adet=1 sözleşmesi **mutasyonla yeniden kanıtlanır**
  - **Derin bağlantı sözleşmesi:** sorgu dizesindeki her süzgeç hedef sayfada **uygulanır** ve kutularda görünür (takvim → `/evraklar` vade süzgeci)
  - Şube seçici P-04'te kurulan şube bağlamını besler; elle süzgeç geri gelmez
  - **`Islemler.razor`'a DOKUNULMAZ** (bağlama P-16'da)
- **Sayaçlar:** `DUZ_SELECT_LIMIT = 0` · `SORGU_SUZGECI_OKUMAYAN_SAYFA = 0` · `HAM_ULID_GIRISI = 0`
- **Tahmin:** 360 ajan-dk · **Risk:** arama ucu şube filtresiyle çakışırsa çapraz şube sonucu döner; negatif test zorunlu.
- **Sürüm:** 1.0

#### P-16 — SATIŞ KALEMİ + FORM ALANI PARİTESİ (Islemler.razor TEK EL)
- **Kimlikler:** Y-4, Y-26, Y-27, B-31
- **Önkoşul:** P-15
- **Dosyalar:** `Pages/Islemler.razor:434-441,1771-1772` (**TEK EL**) · `Pages/Urunler.razor:168-390,576-800` · `Pages/Cariler.razor:235-284,317-367` · `Data/Entities.cs:108` (**`vergi_dairesi` ZATEN VAR — migration listesinden çıkarıldı**) · `Data/Migrations/` · `Kuyumcu.Tests/`
- **Migration:** EVET — `urunler`: tedarikci_id, alis_maliyeti, hedef_marj, kdv_orani, seri_no, olcu, min_stok · `cariler`: unvan, TCKN/VKN ayrımı, dogum_tarihi, uyruk, iskonto, segment. **Mevcut kolonlar (vergi_dairesi, Notlar) önce ölçülür ve listeden düşülür.** `Down()` zorunlu
- **DoD:**
  - Satış kalemi satırında **ayar / milyem / gram / has** görünür; satır ve fiş iskontosu **sunucuda yeniden hesaplanır**
  - **Pasif cariler** satış müşteri listesinde **görünmez**
  - TCKN 11 / VKN 10 hane doğrulaması **alan altında**
  - Alış maliyeti olmadan kârlılık raporu yazılamaz; P-25 buna bağlıdır
- **Sayaçlar:** `DTO_FORM_ESLESMEZ = 0` (kapsam genişler) · `BU_PAKETTE_EKLENEN_RECORD = 0`
- **Tahmin:** 540 ajan-dk · **Risk:** `Islemler.razor` bu dalganın tek eli — P-15 ve P-18 ile **aynı anda koşamaz**. Mevcut kolonu migration'a yazmak boş/çakışan migration üretir (T-5 ihlali).
- **Sürüm:** 1.0

#### P-17 — ORTAK ÇIKTI ŞABLONU + PANO/VİTRİN
- **Kimlikler:** Y-6, Y-7
- **Önkoşul:** P-16, P-01 (R-11 kararı)
- **Dosyalar:** `Bilesenler/Yazdirma/` (YENİ) · `Pages/PusulaBelgesi.razor:213` (**ÖLÇÜLDÜ**) · `Pages/IslemDefteri.razor:307` (**ÖLÇÜLDÜ**) · `Pages/UrunEtiketleri.razor:263` (**ÖLÇÜLDÜ — termal etiket, AYRI çıktı ailesi**) · `Modules/Pano/PanoFiyatServisi.cs:75` · `Modules/ModulKayit.cs` · `Modules/Cari/CariModulu.cs:864`
- **Migration:** ŞARTLI — `PanoKalemAyari` kalıcı ayar tablosu gerekiyorsa EVET (bugün statik)
- **DoD:**
  - Satış fişi, tahsilat makbuzu, emanet teslim belgesi, cari ekstresi **tek A4 şablonundan** üretilir
  - **`UrunEtiketleri` termal/barkod ailesidir:** ortak A4 şablonuna **girmez**, gerekçeli beyaz listede kalır (hasım bulgusu — "3 → 0" hedefi yanlıştı)
  - Pano fiyatı marj/yuvarlama kaydı kiracı ayarından gelir; modül kaydı yapılır
- **Sayaçlar:** `ORTAK_SABLON_DISI_PRINT ∈ gerekçeli beyaz liste`
- **Tahmin:** 360 ajan-dk · **Risk:** R-11 "tasarımcı" derse kapsam 5-7 güne çıkar; karar gelmeden yalnız sabit şablon yazılır.
- **Sürüm:** 1.0

#### P-18 — MOBİL PARİTE + TEMA BELİRTEÇLERİ + STICKY SÜTUN
- **Kimlikler:** B-9, B-30, B-32
- **Önkoşul:** P-16, P-01 (R-13 kararı)
- **Dosyalar:** `Kuyumcu.Web/wwwroot/css/` (belirteçler) · `Bilesenler/KartListe.razor` (YENİ) · `Pages/Cariler.razor`, `Urunler.razor`, `Isletme.razor`, `Banka.razor`, `Islemler.razor` (**yalnız tablo sarmalayıcı**) · `Pages/Yetkiler.razor`, `Fiyatlandirma.razor` · `Bilesenler/` + `Layout/` (**27 + 5 = 32 dosya ölçüm kapsamına alınır**) · `betikler/qa-turu.js`, `betikler/hazirlik-kurulu.js` (**hasım bulgusu R5: sayaç bu betiklerde yaşıyor, dosya listesine eklendi**)
- **Migration:** HAYIR
- **DoD:**
  - **42/42 rotada `scrollWidth ≤ 375`** — bugün: cariler 996, urunler 859, isletme 729, banka 789, islemler 571. **Gerçek tarayıcıda tek tek ölçülür, örnekleme kabul edilmez** (Aşkın koşulu)
  - Karanlık temada vurgu rengi **belirteçten** gelir; sabit renk yazılmaz (`/fiyatlandirma` bugün `rgb(255,251,235)`)
  - `/yetkiler` ROL sütunu **sticky**
  - Ölçüm kapsamı `Pages + Bilesenler + Layout`
- **Sayaçlar:** `MOBIL_YATAY_KAYDIRMA = 0` · `SABIT_RENK` (azalan)
- **Tahmin:** 300 ajan-dk · **Risk:** `Islemler.razor`'a bu paket de dokunur — P-16 ile **asla aynı anda koşmaz**. CSS gerilemesi `denetim.sh`'ta görünmez, yalnız QA turunda yakalanır.
- **Sürüm:** 1.0

**Dalga 4 kapısı:** standart kapı + sayaç birleştirme. **Ek (Eray koşulu):** pilot davet metni + `PILOT-KAYIT.md` bu kapının kontrol listesinde; `PILOT_KISIT_BEYANSIZ_DAVET = 0`.

---

### DALGA 5 — Çıkış kapıları ve defter eksikleri

**Amaç:** paranın/malın dükkândan çıktığı ama defterin görmediği yolları kapatmak.
**Ajan-saat:** 42,6 · **Kurucu gözetim:** ≈1 gün · **Kurucu kalemi:** K-13 (bu dalgadan önce gerekli)

#### P-19 — GİDER + VİRMAN: İKİ BACAKLI ATOMİK DESEN
- **Kimlikler:** Y-12, Y-11
- **Önkoşul:** P-08
- **Dosyalar:** `Data/HareketFabrikasi.cs` · `Data/Entities.cs` (GiderKategorisi; **`:675 karsi_sube_id` MEVCUT** — yeni `karsi_bacak_id` ile ilişkisi yazılı ayrılır) · `Modules/Kasa/KasaModulu.cs`, `Banka/BankaModulu.cs` · `Modules/Gider/` (YENİ) · `Pages/Kasalar.razor`, `Banka.razor`, `Gider.razor` (YENİ) · `Data/Migrations/` · `Kuyumcu.Tests/`
- **Migration:** EVET — `gider_kategorileri` tablosu, `kasa_hareketleri.gider_kategori_id NULL`, `karsi_bacak_id`. `Down()` zorunlu
- **DoD:**
  - Virmanın **iki bacağı tek transaction**; tek bacak yazılırsa test kırılır (mutasyon kanıtı)
  - Storno: virman stornolanırsa **iki bacak birlikte** döner
  - Gider kâr-zararda ayrı satır olur; şubeler arası nakit transferi P-04 filtresinin "iki şube birden" yolunu kullanır
  - `karsi_bacak_id` ile mevcut `karsi_sube_id`'nin **anlam ayrımı belgede yazılı**
- **Sayaçlar:** `ESSIZ_BACAK = 0` · `TEK_SAVECHANGES_DISI_CIFT_BACAK = 0`
- **Tahmin:** 240 ajan-dk · **Risk:** virman kasa/banka bakiyesini çift sayabilir; P-24 mutabakat testi bu paketin kapısıdır.
- **Sürüm:** 1.0

#### P-20 — HURDA SATIŞI / RAMAT ÇIKIŞI
- **Kimlikler:** Y-10
- **Önkoşul:** P-19 · **K-13 kurucu kararı**
- **Dosyalar:** `Modules/Hurda/HurdaModulu.cs:377` · `Data/HareketFabrikasi.cs` · `Data/Entities.cs:28-59` · `Pages/Hurda.razor` · `Kuyumcu.Tests/`
- **Migration:** ŞARTLI — hurda stok bakiyesi ayrı izleniyorsa kolon gerekir (**önce ölçülür**)
- **DoD:** `HURDA_SATIS` has bacağını **eksiltir** ve TL/has karşı bacağı yazar · **K-13 gelmeden fire/ramat farkı yazılmaz**; ekranda "bu sürümde fire kaydı yoktur" gerekçesi görünür · `MadenKapisi` ayarsız hurda yazılmasını engeller
- **Sayaçlar:** `HURDA_STOK_KACAK_YOL = 0`
- **Tahmin:** 180 ajan-dk · **Risk:** K-13 gecikirse bekleyen **tek** paket budur (bilerek izole edildi).
- **Sürüm:** 1.0

#### P-21 — AVANS · SİPARİŞ→SATIŞ · DEĞİŞİM · KONSİNYE · CİRO · REZERV
- **Kimlikler:** Y-17, Y-18, Y-19, Y-20, Y-21, Y-23
- **Önkoşul:** P-19, P-20
- **Dosyalar:** `Data/HareketFabrikasi.cs`, `Entities.cs`, `Siparis.cs` · `Modules/Siparis/SiparisModulu.cs:55,733-767` · `Modules/Satis/SatisModulu.cs` · `Modules/Evrak/EvrakModulu.cs:853-918` · `Data/Migrations/` · `Kuyumcu.Tests/`
- **Migration:** EVET — `hareketler.siparis_id`, `stoklar.bloke_adet DEFAULT 0`, evrak ciro durumu + rücu alanı. `Down()` zorunlu
- **DoD:**
  - Sipariş → satış **tek transaction**; yarım dönüşüm imkânsız (mutasyon kanıtı)
  - Avans satışa mahsup edilince cari bakiye **çift saymaz**
  - Bloke adet satılabilir stoktan düşer; sayım raporu blokeyi ayrı gösterir; `stok.adet < bloke_adet` testte 0
  - Ciro edilen evrakta **rücu durumu terminal değildir**
  - **1.1 için yer bırakılır:** taksit (Y-24) ve emanet parça kimliği (Y-22) tasarımı engellenmez
- **Sayaçlar:** `TERMINAL_SONRASI_YAZAN_YOL = 0` · `BLOKE_TUTARSIZ = 0`
- **Tahmin:** 420 ajan-dk · **Risk:** altısı da `HareketFabrikasi`'na dokunduğu için **bilerek tek paket**; bölünürse aynı dosyaya iki el değer. Değişim storno motoruyla karışırsa çift iade riski.
- **Sürüm:** 1.0

#### P-22 — SATIŞ / FATURA / SİPARİŞ / SAYIM / POS EKRAN BORÇLARI
- **Kimlikler:** B-3, B-26, B-33, B-44, B-45, B-46, B-55, B-56
- **Önkoşul:** P-15, P-21
- **Dosyalar:** `Pages/Faturalar.razor`, `Siparisler.razor`, `Sayim.razor`, `KasaRaporu.razor` (**ÖLÇÜLDÜ: `Pages/Raporlar/` dizini YOK**), `Pos.razor` · `Modules/Fatura/FaturaModulu.cs`, `Siparis/SiparisModulu.cs`, `Pos/` · `Kuyumcu.Tests/`
- **Migration:** ŞARTLI — gün sonu kasa sayımı için ayrı tablo gerekiyor mu, mevcut `GunKapanisi.cs` yeterli mi **önce ölçülür**
- **DoD:**
  - **TASLAK fatura ve kalemleri düzenlenebilir**; numara serisi yalnız **kesildiğinde** tüketilir (tek nokta)
  - Sipariş durum geçişleri **onay ister ve geri alınabilir**; ATOLYEDE/HAZIR geri dönüşü **K-23 kararına bağlıdır**, gelmeden açılmaz ve ekranda gerekçe yazılır
  - `/sayim` DURUM sütunu gerçek durumu yazar (düz metin "İptal Et" kalkar); sayım oturumunun **adı** düzenlenebilir
  - Gün sonu kasa sayımı girilebilir (`/raporlar` kasa SAYILAN NAKİT ve FARK dolu)
  - **B-46/B-55:** POS "Bağlı Kasa" düzeltme yolu + tahsilat varken **"yeni kasaya taşı" göç akışı**; **B-56:** tahsilatlı ret 5198'de **canlı** denenir (S-3f §11.3 bugüne dek ölçülmedi)
- **Sayaçlar:** `SERI_TUKETIM_NOKTASI = 1` · `DURUM_SUTUNU_EYLEM_METNI = 0`
- **Tahmin:** 300 ajan-dk · **Risk:** POS tahsilatı fikstürü gerekir; tek kullanımlık kiracı adı `PAKET*/TEST*/QA*` kuralına uyar, **sonunda silinir**.
- **Sürüm:** 1.0

---

### DALGA 6 — Büyük yapısal (en riskli dalga)

**Amaç:** ödeme kalemi/vezne genelleştirme, mutabakat sözleşmesi, ölçek-kültür borcu ve tedarik/muhasebe köprüsü.
**Ajan-saat:** 58,0 · **Kurucu gözetim:** ≈1,5 gün · **Kurucu kalemi:** K-10 (bu dalgadan önce gerekli)

#### P-23 — ÖDEME KALEMİ TABLOSU + VEZNE FİŞİNİN GENELLEŞTİRİLMESİ
- **Kimlikler:** R-17, R-18
- **Önkoşul:** P-21, P-01
- **Dosyalar:** `Data/VezneFisi.cs:14-17,56,127` · `Data/HareketFabrikasi.cs`, `Entities.cs` · `Modules/Vade/VadeKapisi.cs:172` · `Modules/Satis/SatisModulu.cs`, `StornoServisi.cs` · `Modules/Kasa/KasaModulu.cs` · `Data/Migrations/` · `Kuyumcu.Tests/`
- **Migration:** EVET — `odeme_kalemleri` (fis_id, yöntem, tutar, para birimi, pos_tahsilat_id, evrak_id) + KARMA ödemelerin geri doldurulması. Önce salt okunur etki ölçümü; `Down()` tabloyu düşürür, eski alanları **korur**
- **DoD (mimari VETO şartları — Bertan K3 + Mehmet koşulu):**
  - Geri doldurma migration'ı ancak **(i)** salt okunur etki raporu, **(ii)** gerçekten koşturulmuş `Down()` tatbikat çıktısı, **(iii)** öncesi/sonrası tam DB yedeği + **geri dönüş provası kaydı** — üçü de rapora girdikten sonra koşar
  - Vezne fişi başlığı genelleşir ama **vezne hâlâ deftere YAZMAZ** (`VezneFisi.cs:14-17` kuralı korunur); `SatisServisi` tek kapı kalır
  - **R-18 mahsup:** ya **tüm** ödeme yollarını kapsar ya **hiç** girmez — kısmi mahsup bakiyeyi saptırır, **mimari veto**
  - Storno motoru **ödeme kalemlerini de döndürür**; unutulursa para tek yönlü kaybolur
  - MASAK kümülatifi ve 459 s. VUK tevsik haddi yeni tablo üzerinden **aynı** sonucu verir (gerileme testi)
  - Geri doldurmada belirsiz satır **"BİLİNMİYOR"** işaretlenir, tahmin edilmez (Anayasa md.1); sayısı raporda açık
  - 1.1 taksit planı (Y-24) bu tabloya sonradan eklenebilecek biçimde tasarlanır
  - **Alt-adım kırılımı zorunlu** (Mustafa K-F7): ölçüm / migration / test / geri dönüş provası dakikaları ayrı yazılır
- **Sayaçlar:** `HAREKETLERDE_ODEME_YONTEMI_YENI_YOL = 0` · `FIS_TOPLAM_ODEME_KALEMI_FARKI = 0`
- **Tahmin:** 600 ajan-dk · **Risk: EN YÜKSEK.** Bakiye, tevsik ve MASAK üçünü birden etkiler. Yedek + geri dönüş provası olmadan koşulmaz.
- **Sürüm:** 1.0

#### P-24 — MUTABAKAT SÖZLEŞMESİ + KANIT REJİMİ
- **Kimlikler:** B-7, B-28
- **Önkoşul:** P-04, P-19, P-23
- **Dosyalar:** `Modules/Rapor/*.cs` · `Modules/Kasa/KasaModulu.cs`, `Evrak/EvrakModulu.cs` · `Kuyumcu.Tests/` (mutabakat testleri) · `betikler/qa-turu.js` (**getBBox ölçümü**)
- **Migration:** HAYIR
- **DoD:**
  - Her özet alanı, altındaki listenin toplamıyla **aynı sorgudan** türer; ayrı hesap yasak
  - Vezne ₺0,00 ↔ detay 2000 · evrak portföyü boş ↔ 2 evrak · kâr-zarar 11,45 g ↔ 50,38 has çelişkileri **tek tek kapanır**
  - Grafik kanıtı path dizgesi değil **`getBBox` genişliği** ile ölçülür (tek kategori %100 çizilmiyor bulgusu)
- **Sayaçlar:** `OZET_AYRI_SORGU = 0` · `MUTABAKAT_TESTI_OLMAYAN_OZET = 0`
- **Tahmin:** 240 ajan-dk · **Risk:** P-04 ve P-23'ten önce koşarsa aynı testler iki kere yazılır.
- **Sürüm:** 1.0

#### P-25 — ÖLÇEK + KÜLTÜR + RAPOR TAMAMLAMA + YÜK TESTİ
- **Kimlikler:** B-8, Y-16, Y-31, R-6
- **Önkoşul:** P-24, P-16
- **Dosyalar:** `dotnet/denetim.sh:694-731` (**ÖLÇÜLDÜ: `KULTURSUZ_TAVAN=44` sat.729, `RAPOR_MAT_BEKLENEN=43` sat.706**) · `Modules/Rapor/*.cs`, `Maliyet/*.cs`, `Rapor/RaporKatalogu.cs:41-68` · `Modules/Rapor/SatisRaporlari.cs` · `dotnet/yuk-testi/k6-senaryo.js` · `Pages/Raporlar.razor`, `KarZarar.razor`, `SatisAnalizi.razor`, `KasaRaporu.razor`
- **Migration:** HAYIR
- **DoD:**
  - 171 sınırsız `ToListAsync` azalan hedefe; `GunlukOzet` / `AylikOzet` / `PosFaturaMutabakati` **sunucuda toplar**
  - 44 kültürsüz biçim azalan hedefe; beyaz liste (CariYaslandirma, StokDegeri) **tarihli ve azalan**
  - **16. rapor** (stok yaşlandırma / devir hızı) eklenir, `FazAAsgari` 15 → 16; rapor sayısı **katalogdan** yazılır, belgeden değil
  - Personel raporu **"KAYDEDEN bazlı"** adıyla çıkar (`SatisRaporlari.cs:20-28` kendi notu); "satıcı bazlı" iddiası ancak P-08'in `personel_id = 'SATAN'` kilidi kanıtlandıktan sonra kullanılır
  - **k6 yük testi** 50k ürün + 3 yıl hareketle **B-8'den SONRA** koşulur; ham çıktı `ham/yuk-testi-<tarih>.json` olarak sürümlenir
- **Sayaçlar:** `SINIRSIZ_TOLIST` (azalan) · `KULTURSUZ_TAVAN` (azalan) · `BEYAZ_LISTE_BOYU` (raporlanır, kalıcılaşamaz)
- **Tahmin:** 420 ajan-dk · **Risk:** yük testi B-8'den önce koşarsa sonuç yanıltıcı olur; P-04 şube filtresini de sınar, indeks eksikse burada patlar.
- **Sürüm:** 1.0

#### P-26 — TEDARİK ZİNCİRİ + MUHASEBE KÖPRÜSÜ + ÖZEL MATRAH
- **Kimlikler:** Y-34, Y-35, Y-37, Y-30, Y-36, Y-40
- **Önkoşul:** P-21, P-16 · **K-13** (Y-36 dilimi) · **K-10** (Y-40 ve muhasebe biçimi)
- **Dosyalar:** `Modules/Satinalma/` (YENİ), `Modules/Irsaliye/` (YENİ) · `Modules/Disaaktarim/Kaynaklar.cs` · `Modules/Siparis/SiparisModulu.cs:55` · `Modules/Fatura/OzelMatrahMotoru.cs:45` · `Data/Entities.cs:881` (`ozel_matrah_kodu`) · `Data/Migrations/` · `Kuyumcu.Tests/`
- **Migration:** EVET — `satinalma_siparisleri` + kalemleri, `irsaliyeler` + kalemleri, `siparis.usta_cari_id`, hesap kodu alanı. `Down()` zorunlu
- **DoD:**
  - Mal kabul stoğa **`HareketFabrikasi` üzerinden** yazar; irsaliye faturaya bağlanır, bağsız irsaliye raporda görünür
  - Muhasebe fişi (CSV/XML) **K-10 teyidine bağlı satırları BOŞ bırakır, uydurmaz**
  - Tamir iş emrindeki kapora **gerçek tahsilat hareketi** üretir (bugün KISMİ/OPSİYONEL)
  - **Y-36** ustaya metal çıkışı **K-13 gelmeden yazılmaz**
  - **Y-40 KDV 23/e:** motor ve kolon **hazır**; K-10 teyidi gelirse tek satır yapılandırmayla açılır ve hangi faturaya uygulandığı **ekranda görünür**. Teyit gelmezse ürün içinde **"HARİÇ" beyanı** yazılır (hasım bulgusu: bu kalem sahipsizdi)
- **Sayaçlar:** `DEFTERE_YAZMAYAN_PARA_MODULU = 0` (vezne istisnası gerekçeli beyaz listede) · `OZEL_MATRAH_BEYANSIZ_EKRAN = 0`
- **Tahmin:** 540 ajan-dk · **Risk:** muhasebe biçimi mali müşavir teyidi olmadan "doğru" iddia edilemez.
- **Sürüm:** 1.0

**Dalga 6 kapısı:** standart kapı + sayaç birleştirme. **Ek:** P-23 öncesi ve sonrası **tam DB yedeği + geri dönüş provası** raporda.

---

### DALGA 7 — Ticari ayak + belge hattı (KOŞULSUZ 1.0)

**Amaç:** F-1 + F-3 (para tahsili, plan kapısı) ve F-4 (telemetri) olmadan ürün hiçbir senaryoda satılamaz. **Belge / pazarlama / pilot hattı kod hattıyla PARALEL koşar.**
**Ajan-saat:** 47,5 · **Kurucu gözetim:** ≈1,5 gün · **Kurucu kalemi:** K-1 (bu dalgadan önce gerekli)

#### P-27 — F-1 ABONELİK + F-3 PLAN KAPISI
- **Kimlikler:** R-1
- **Önkoşul:** P-09 (K3 dilimi orada kapandı), P-23
- **Dosyalar:** `Modules/Admin/AdminModulu.cs:161-169` · `Modules/Abonelik/` (YENİ) · `Data/Migrations/` · `Kuyumcu.Tests/`
- **Migration:** EVET — abonelik ve plan tabloları. **Şema dilimi FK paketinden (P-34) ÖNCE biter** ki FK grubu bu tabloları da kapsasın (hasım bulgusu)
- **DoD:** Plan değişimi yetki kapısından geçer, kiracı kendi planını yükseltemez · plan değişimi **canlı denetim izi** üretir ve bu izin varlığı kanıtlanır (kurul md.2: kanıtlanmadan ücretli sözleşme imzalanmaz) · abonelik durumu ürün davranışını **fail-closed** etkiler (süresi dolan kiracı yazma yapamaz, **veri kaybetmez**)
- **Sayaçlar:** `YETKISIZ_PLAN_YAZAN_UC = 0` · `PLAN_DEGISIMI_IZSIZ = 0`
- **Tahmin:** 300 ajan-dk · **Risk:** abonelik tabloları FK borcunu yeniden üretmemeli — P-34 önkoşuluna eklendi.
- **Sürüm:** 1.0

#### P-28 — F-4 KİRACI ETİKETLİ TELEMETRİ + DESTEK DAKİKASI KAYDI
- **Kimlikler:** R-2
- **Önkoşul:** P-27
- **Dosyalar:** `Kuyumcu.Api/Program.cs` · `Modules/Telemetri/` (YENİ) · `dokumanlar/02-mimari/F-8-BIRIM-EKONOMI-2026-09-16.md`
- **Migration:** ŞARTLI — destek dakikası kaydı için tablo gerekirse EVET
- **DoD:** Kiracı etiketli sayaçlar (istek, CPU, depolama) toplanır; **sır/kişisel veri taşımaz ve bu denetimle kanıtlanır** (Mehmet koşulu) · F-8 Faz B'nin iki boş satırı (kapasite, destek) bu telemetri + k6 (P-25) + pilot (P-32) ile **doldurulur** · **rakam ölçülmeden F-8'e yazılmaz** (FAIL-CLOSED 1 aktif kalır)
- **Sayaçlar:** `TELEMETRIDE_KISISEL_VERI = 0` · `F8_OLCULMEMIS_SATIR = 0` (dalga sonunda)
- **Tahmin:** 180 ajan-dk · **Risk:** telemetri kiracı verisini sızdırırsa **Mehmet vetosu**.
- **Sürüm:** 1.0

#### P-29 — S-1 TİCARİ PAKET + ÜRÜN ANLATIMI *(BELGE HATTI — PARALEL)*
- **Kimlikler:** R-3, R-8, D-8
- **Önkoşul:** P-28 (rakamlar için)
- **Dosyalar:** `docs/HIZLI-BASLANGIC.md` (YENİ) · `docs/SLA.md` (YENİ) · `docs/HARIC-LISTESI.md` (**YENİ — Köprü K-1 koşulu: kısıt beyanının TEK KAYNAĞI**) · `dokumanlar/09-uygulama-kayitlari/PAKET-S-1-uygulama-raporu.md` (YENİ)
- **Migration:** HAYIR
- **DoD:**
  - SLA yalnız **bugün taahhüt edilebileni** yazar (yedek sıklığı, geri yükleme hedefi P-33 tatbikat kanıtına dayanır); kanıtı olmayan sayı yazılmaz
  - Hızlı başlangıç kurulumdan ilk satışa: süre **tahmin değil, ölçülür** — **iki kez** (biri ekip üyesi, biri ürünü hiç görmemiş kişi; Köprü K-4)
  - **R-8:** tek rakamlı "silme/tasarruf oranı" **hiçbir kanalda ilan edilmez**; konumlandırma toplam sahip olma maliyetine dayanır (F-8: Dezy 500 ₺ karşısında fiyat üstünlüğü YOK, M&L %85 iddiası ≈%54)
  - 1.0'da **HARİÇ** olanlar `HARIC-LISTESI.md`de tek kaynakta listelenir: e-Fatura (K-2), ÖKC (K-11), SMS (K-15), banka API (K-22), imalat (Katman 1). **Göç (rakip programdan veri alma) kararı da buraya yazılır** (Köprü K-6: ya CSV ile cari+ürün+açılış bakiyesi içe aktarımı 1.0'a alınır ya "göç yoktur, veri elle girilir" yazılır — sessiz bırakılamaz)
  - Belge **"TASLAK — ilan edilmemiştir"** damgası taşır; rakamlı fiyat ilanı **K-16 kurucu imzasına** bağlıdır
- **Sayaçlar:** `HARIC_BEYAN_KOPYA = 0` (aynı kısıt metninin ikinci elle yazılmış kopyası yok) · kapı: `PAKET-S-1` raporu olmadan sürüm mührü atılmaz
- **Tahmin:** 240 ajan-dk · **Risk:** belge hattı kod hattından **bağımsız** koşar; seri hatta sıraya konmaz.
- **Sürüm:** 1.0

#### P-30 — DEMO-1 TEMİZ DEMO KİRACISI + 15 DK GÖSTERİM
- **Kimlikler:** R-4
- **Önkoşul:** P-24 (mutabakat kapanmadan demo tohumlanmaz) · **K-1**
- **Dosyalar:** `betikler/demo-tohumla.js` (YENİ) · `betikler/kiraci-temizle.sh` · `docs/RUNBOOK-DEMO.md` (YENİ)
- **Migration:** HAYIR
- **DoD:**
  - Demo `QA-TEST-*` fikstüründen **türetilmez**; adı `DEMO-*` olur ve `kiraci-temizle.sh`'ın silme ad kuralına **eklenmez**
  - 3 aylık tutarlı geçmiş **`HareketFabrikasi` üzerinden** tohumlanır, doğrudan INSERT ile değil
  - 15 dakikalık gösterim betiği **iki ayrı ajan tarafından iki kez** baştan sona koşulur, süre ölçülür ve `ham/demo-prova-<tarih>.json`'a yazılır; **> 20 dk ise paket kapanmaz** (Eray koşulu)
  - Kurgusal adlar kullanılır (KVKK algısı)
  - Hariç listesi demo kiracısının **giriş ekranında okunur** (Eray koşulu)
- **Sayaçlar:** `DEMO_IZOLASYON` · `DEMO_ROTA_KRITIK = 0` (gösterim betiğinin dokunduğu her rota, iki ardışık turda kritik = 0)
- **Tahmin:** 180 ajan-dk · **Risk:** tohumlanan geçmiş iş kuralını ihlal ederse demo ürünün en kötü reklamı olur.
- **Sürüm:** 1.0

#### P-31 — T-4b MASKELİ DESTEK PAKETİ + /destek + HATA KİMLİĞİ
- **Kimlikler:** R-5
- **Önkoşul:** P-02, P-28
- **Dosyalar:** `Kuyumcu.Api/Common/ApiException.cs` · `Bilesenler/HataBandi.razor`, `KabukHatasi.razor` · `Pages/Ayarlar.razor`
- **Migration:** HAYIR
- **DoD:** Her beklenmeyen hata **kısa hata kimliği** gösterir; aynı kimlik sunucu günlüğünde bulunur; kimlik sır/kişisel veri taşımaz · tanılama paketinin sır/kişisel veri içermediği **denetimle** kanıtlanır (fail-closed) · destek dakikası bu kanaldan P-28 sayacına yazılır · **Mehmet'in kendi turunda da doğrulanır** (koşul)
- **Sayaçlar:** `TANILAMA_PAKETINDE_SIR = 0` · `IC_KOD_SIZINTISI = 0` (P-13'ten devralınır)
- **Tahmin:** 60 ajan-dk · **Risk:** hata kimliği günlüğe kişisel veri yazma cazibesi yaratır; eşleşme yalnız sunucu tarafında.
- **Sürüm:** 1.0

#### P-32 — PİLOT-1: ÜCRETSİZ PİLOT DAVETİ, YAZILI KISIT BEYANI, DESTEK ÖLÇÜMÜ
- **Kimlikler:** Y-41, R-7
- **Önkoşul:** P-28, P-30 · **metin dilimi DALGA 3-4 kapısında hazır** (Ramazan K5, Köprü K-2)
- **Dosyalar:** `docs/RUNBOOK-PILOT-DAVET.md` (**ÖLÇÜLDÜ: MEVCUT, 2026-09-17 — temel alınır, yeniden yazılmaz**) · `dokumanlar/09-uygulama-kayitlari/PILOT-KAYIT.md` (YENİ)
- **Migration:** HAYIR
- **DoD:**
  - Kurul md.8: bugün dışarıya verilebilecek **tek ticari biçim**, kısıtları yazılı bildirilen **ücretsiz pilottur** — davet metni kısıtları `HARIC-LISTESI.md`den gömerek sayar
  - Pilot destek dakikası P-28 telemetri dilimiyle **aynı kayda** yazılır; F-8 Faz B destek satırının **tek meşru kaynağıdır**
  - **Haftalık ölçüm kaydı** (Köprü K-3): pilot sayısı, açılan destek kaydı, ortalama ilk yanıt süresi, kapanan/açık. **Boş hafta satırı = 0**; F-8 en az **4 haftalık** gerçek veriyle dolar, tek haftadan ekstrapolasyon yasak
  - `PILOT-KAYIT.md`de "davet metni hazır tarihi" ve "kurucuya sunuldu tarihi" dolu; boşsa DALGA 7 kapısı açılmaz
  - Pilot 4-6 hafta paralel yürür; **1.0'ı bloklamaz** ama "ücretli tarih" ancak pilot + iki ardışık temiz tur sonrası verilir
- **Sayaçlar:** `PILOT_KISIT_BEYANSIZ_DAVET = 0`
- **Tahmin:** 180 ajan-dk · **Risk:** pilot davetinin **gönderilmesi dış iletişimdir** — metni ekip hazırlar, gönderimi kurucu yapar (K-19).
- **Sürüm:** 1.0

#### P-33 — KILAVUZUN TEK KAYNAKTAN ÜRETİMİ + TEMİZ KURULUM + YEDEK TATBİKATI
- **Kimlikler:** B-11, B-5
- **Önkoşul:** P-14, P-18, P-09
- **Dosyalar:** `docs/KULLANIM_KILAVUZU.md` (**TEK KAYNAK**) · `Kuyumcu.Web/wwwroot/kilavuz.html` (**ÜRETİLİR**) · `Pages/Kilavuz.razor` · `docs/DOTNET.md:36`, `docs/MIMARI.md:53,336` (localhost:5000 / start.bat temizliği) · `dotnet/kurulum.sh`, `kurulum.ps1`, `yayin-al.sh`, `yedek-al-ve-dogrula.sh`, `yedek-ortak.sh` (**ÖLÇÜLDÜ: hepsi MEVCUT, yeniden yazılmaz**) · `docs/DEPLOY.md`, `docs/YEDEK-GERI-YUKLEME.md` · `docs/RUNBOOK-TESLIMAT.md` (**YENİ — Köprü K-7**)
- **Migration:** HAYIR
- **DoD:**
  - Kılavuz `RotaBasliklari`'ndan **üretilir**: 42 ekranın hepsi var (bugün `kilavuz.html` 18-39 başlık, 13 ekran hiç yok)
  - Eski port/adres referansları kalkar; kullanıcı belgesine **sabit port yazılmaz**
  - Temiz makinede sıfırdan kurulum tatbikatı **kaydedilir**; mevcut kurulum betikleri kullanılır. Kurucu kalemleri (K-6/K-9) yoksa başlık **"YEREL KAP TATBİKATI"** olur, **"üretim kanıtı" DENMEZ**
  - Yedek + geri yükleme tatbikatı **checksum karşılaştırmasıyla** koşulur; son tatbikat tarihi `/yedekler` ekranında görünür ve **30 günden eski olamaz** (Köprü K-5)
  - Oturumsuz erişim kararı yazılı; `Kilavuz.razor` **iframe kullanmaz** (Mehmet kararı korunur)
  - **K-26 gelmezse** kısayol/adres bölümü **koşullu** yazılır
  - **`RUNBOOK-TESLIMAT.md`** (kurulum adımı, adres/kısayol, destek kanalı, yedek anahtarı K-18) bu pakette toplanır; **bu dosya olmadan P-35 sürüm mührü atılmaz**
- **Sayaçlar:** `KILAVUZ_EKSIK_EKRAN = 0` · `KILAVUZ_URETILMIS` · `ESKI_PORT_REFERANSI = 0` · `YEDEK_TEK_YOL` · `YEDEK_TATBIKAT_BAYAT = 0`
- **Tahmin:** 210 ajan-dk · **Risk:** menü/ekran değiştikçe kılavuz bayatlar — bilerek **ekran paketlerinden sonra**.
- **Sürüm:** 1.0

---

### DALGA 8 — Referans bütünlüğü ve kapanış

**Amaç:** tüm şema eklemeleri bittikten **sonra** gerçek FK'lar; ardından iki ardışık temiz turla sürüm mührü.
**Ajan-saat:** 42,6 · **Kurucu gözetim:** ≈1 gün

#### P-34 — REFERANS BÜTÜNLÜĞÜ: GERÇEK FK'LAR (EN SON)
- **Kimlikler:** Y-28
- **Önkoşul:** P-05, P-08, P-19, P-21, P-23, P-26, **P-27 (abonelik şeması dâhil)**, **K-1**
- **Dosyalar:** `Data/KuyumcuDbContext.cs` (**ÖLÇÜLDÜ: 135 HasIndex, 0 HasOne/WithMany**) · `Data/Entities.cs` · `Data/Migrations/` (**ÖLÇÜLDÜ: 107 .cs, 8'i 0 bayt — sayı ARTAMAZ**) · `dotnet/yetim-tara.sh` (**ÖLÇÜLDÜ: MEVCUT, 13 Eylül, 6.434 bayt, ADR-0009 ön şartı — GENİŞLETİLİR, yeniden yazılmaz**) · `Kuyumcu.Tests/`
- **Migration:** EVET — aşamalı: **(1)** yetim ölçümü (`yetim-tara.sh`, salt okunur), **(2)** kompozit `(tenant_id, x_id)` FK'lar **gruplar hâlinde**, **(3)** her grup ayrı migration + ayrı `Down()`. **Tek dev migration YASAK**
- **DoD:**
  - **İlk iş:** mevcut `yetim-tara.sh` koşulur, **bugünkü çıktısı rapora girer**; eksikse genişletilir
  - **Bertan K6 / Mehmet koşulu:** toplam yetim = 0 **veya** her yetim grubu için kurucu kararı (K-1) yazılı. Yetim varken FK konmaz
  - ADR-0009 kompozit FK deseni uygulanır; silme davranışı **RESTRICT**, **cascade YASAK**
  - Her migration'ın `Down()`'ı **gerçekten koşturulur** ve çıktısı raporda verilir (tatbikat, iddia değil)
- **Sayaçlar:** `FK_SIZ_ID_KOLONU` (azalan; taban P-02'de ölçülür) · `SIFIR_BAYT_MIGRATION ≤ 8` (-le, artış FAIL)
- **Tahmin:** 900 ajan-dk (**alt-adım kırılımı zorunlu — Mustafa K-F7**) · **Risk:** önce konursa sonraki her şema paketi FK'ları yeniden yazar. Yedek + geri dönüş provası olmadan koşulmaz.
- **Sürüm:** 1.0

#### P-35 — KAPANIŞ: İKİ ARDIŞIK TEMİZ TUR + SÜRÜM MÜHRÜ + DEVİR
- **Kimlikler:** R-9 (kapanış mührü), Y-42 (triyaj kaydı kapanışı)
- **Önkoşul:** P-34, tüm dalgalar
- **Dosyalar:** `ham/surum.json` · `ORKESTRATOR-DURUM.md` · `KAPSAM-ENVANTERI.md` · `hafiza/SESSION-HANDOFF.md`
- **Migration:** HAYIR
- **DoD:**
  - 42/42 rota taranmış **iki ardışık turda** kritik = 0 ve düzenlenemeyen = 0; ikinci tur **değişikliksiz derlemede** koşar
  - `denetim.sh` TEMİZ, tam süit 0 başarısız, 0 uyarı 0 hata; her mekanizma kaleminin cırcır sayacı yeşil. **Ham komut çıktısı saklanır** (özet/ekran görüntüsü değil — Aşkın koşulu)
  - Açık kalan her kimlik {1.1 · kurucu kalemi · gerekçeli kapsam dışı} kümesinden birine yazılı; **damgasız bulgu kalmaz**
  - Test kiracıları (`PAKET*/TEST*/QA*`) silinmiş (Aşkın sabit kalite kapısı)
  - **"Ücretli tarih" cümlesi** ancak bu iki temiz tur + pilot destek ölçümü sonrası kurulabilir
- **Sayaçlar:** `KAPI_IKI_ARDISIK_TEMIZ_TUR = 1` (aksi hâlde sevk YOK)
- **Tahmin:** 240 ajan-dk · **Risk:** ikinci tur kirlenirse sayaç sıfırlanır ve tur yeniden başlar — **pazarlık yok**.
- **Sürüm:** 1.0

---

## § 3 — KURUCU ONAYIYLA GİREN BLOK (iki senaryo)

> **BU BLOK KÜÇÜLDÜ.** Taslak; F-1/F-3, F-4, S-1/DEMO-1 ve T-4b'yi "kurucu onayı yoksa koşmaz" diye ayrı bir senaryoya koymuştu. **Hasım bulgusu kabul edildi:** kurul tur 4 mührü bu kalemleri zaten 1.1'den 1.0'a çekmişti ve o senaryoda ürün **hiçbir zaman satılamazdı.** Hepsi DALGA 7'de **koşulsuz 1.0**'dadır (P-27, P-28, P-29, P-30, P-31, P-32).

Kurucu onayına kalan **tek ticari kalem: K-16 — rakamlı FİYAT İLANI.**

### Senaryo A — K-16 zamanında gelirse

| Aşama | Ne olur | Takvim |
|---|---|---|
| Hafta 1-15 | 9 dalga, 36 paket kod hattı | 11-15 hafta |
| Hafta 4-10 (paralel) | Belge hattı + pilot metni hazır, pilot daveti kurucuya sunulur | paralel, takvimi uzatmaz |
| Pilot penceresi | 4-6 hafta, DALGA 4 kapısından itibaren başlayabilir | kod hattıyla örtüşür |
| Kapanış | İki ardışık temiz tur (P-35) | +1 hafta |
| **Fiyat ilanı** | K-16 imzasıyla, F-8 dolu + iki temiz tur şartı sağlandıktan sonra | **kapanışla aynı hafta** |

**Sonuç: ürün satışa hazır ilan edilebilir ≈ 12-16 hafta.**

### Senaryo B — K-16 gelmezse

| Aşama | Ne olur | Takvim |
|---|---|---|
| Hafta 1-15 | Aynı — **hiçbir paket durmaz** | 11-15 hafta |
| Kapanış | Aynı | +1 hafta |
| Ticari çıktı | Fiyat kademesi ve SLA **"TASLAK — ilan edilmemiştir"** damgasıyla hazır bekler | — |
| Dışarıya verilebilen | **Yalnızca kısıtları yazılı ücretsiz pilot** (P-32). Ücretli sözleşme imzalanamaz | süresiz |

**Takvim farkı: SIFIR.** K-16 kod hattının hiçbir yerini bloklamaz; yalnız **son adımı** (rakam yayımlama) bloklar. Ürün teknik olarak hazır olur, ticari olarak rafta bekler.

### İlanın ön şartları (ekip tarafı, hepsi plan içinde)
1. F-8 Faz B'nin iki boş satırı P-28 telemetri + P-25 k6 + P-32 pilot ölçümüyle **doldurulmuş** olmalı
2. **İki ardışık temiz QA turu** (P-35) görülmüş olmalı
3. P-29'daki `HARIC-LISTESI.md` yazılı olmalı
4. Pilot **en az 4 haftalık** gerçek destek verisi üretmiş olmalı (Köprü K-3)

### Kurucu imzası isteyen dış işler (ekip metni hazırlar, göndermez)
Pilot davetinin **gönderilmesi** (K-19) · ücretli sözleşme imzası · para/harcama taahhütleri.

---

## § 4 — KURUCU KALEMLERİ (dış bağımlı, ekip uygulamaz)

| Kimlik | Konu | İhtiyaç tarihi | Gelmezse ne durur |
|---|---|---|---|
| K-1 | Canlı DB kiracı sızıntı satırlarının silinmesi + olay kaydı + QA fikstür temizliği | DALGA 7 öncesi | P-34 (FK) başlayamaz; P-30 demo kirli tabanda kurulur |
| K-2 | e-Fatura/e-Arşiv entegratör sözleşmesi + mali mühür + kontör | P-29 öncesi | 1.0'da "hariç" beyanı (`HARIC-LISTESI.md` ve ürün içinde) |
| K-3 | Kur verisi için yazılı izin/abonelik | DALGA 3 öncesi | Pilot varsayılanı TCMB + elle has/gram |
| K-4 | Depo temizliği (5 `.mutorj` Pages'te + 11 diğer, 8 adet 0 baytlık migration .cs, `.ek`) | DALGA 1 öncesi | Ekip yalnız sayaç kurar, silmez; grep tabanlı sayaçlar kirli kalır |
| K-5 | Sır rotasyonu (JWT sırrı, MySQL parolası, bir kez ekrana basılan geliştirme sırrı) | **DALGA 1 öncesi** | Yayın **fail-closed durur** (Mehmet koşulu) |
| K-6 | Bulut hesabı + ödeme yöntemi + KDV-2 teyidi | P-33 öncesi | Tatbikat "YEREL KAP TATBİKATI" başlığıyla kalır |
| K-7 | 40-B: KVKK aydınlatma metni onayı + ad alanı şifreleme kararı | DALGA 7 öncesi | Aydınlatma metni yayımlanamaz. Metnin hangi belgeden geldiği **doğrulanmalı** |
| K-8 | Commit kararı (bin/obj depoda, yüzlerce değişiklik çalışma ağacında) | Her an | Talimat değişmedikçe **commit yok** |
| K-9 | Alan adı + TLS + yaptırım listesi veri sağlayıcı bedeli | P-33 / T-1 öncesi | Üretim kurulumu tatbikatı yerel kalır |
| K-10 | Mali müşavir: KMTS beyanı, KDV 23/e özel matrah kodu teyidi, vade farkı KDV'si | **DALGA 6 öncesi** | P-26'daki Y-40 anahtarı **açılmaz**; muhasebe fişinin "doğru" iddiası kurulamaz |
| K-11 | ÖKC / Z raporu cihaz kararı | P-29 öncesi | "Hariç" beyanı |
| K-12 | Emanet/rehin hukuki görüşü | 1.1 | Y-22 emanet parça kimliği **açılmaz** |
| K-13 | Katman 1 / fire sınırı | **DALGA 5 öncesi** | P-20 bekler; P-26'nın Y-36 dilimi yazılmaz |
| K-14 | İkinci el / bandrol kapsamı | 1.1 | D-1'in seri no/bandrol dilimiyle **çelişir**; çelişki burada karara bağlanır |
| K-15 | SMS sağlayıcı | 1.1 | D-7 mesaj gönderimi yok; "hariç" beyanı |
| K-16 | **Rakamlı fiyat İLANI** | En sonda | Ürün hazır, fiyat ilan edilemez (§ 3 Senaryo B) |
| K-17 | Rakip deneme hesapları (göç provası için) | Köprü K-6 kararına bağlı | Göç yolu doğrulanamaz |
| K-18 | Yedek anahtarının sunucu dışında saklanması | P-33 öncesi | `RUNBOOK-TESLIMAT.md` eksik kalır |
| K-19 | Dış iletişim / gönderim imzası (pilot daveti dâhil) | DALGA 4 sonrası | Pilot başlamaz → F-8 destek satırı boş kalır → ücretli tarih verilemez |
| K-20 | **iCloud eşitlemesi** | **Hemen, DALGA 0 öncesi** | **Tüm takvim geçersiz** (açıkken tek derleme 30 dk ölçülmüştü) |
| K-21 | RFID / PVC donanım | 1.1 | P-10'da "donanım kapsam dışı" beyanı |
| K-22 | Canlı banka API | 1.1 | "Hariç" beyanı |
| K-23 | **(YENİ)** Sipariş ATOLYEDE/HAZIR durumundan **geri dönüş** kararı (S-3f §12.2) | DALGA 5 öncesi | P-22'nin sipariş durum dilimi bu yolu açmaz, ekranda gerekçe yazar |
| K-24 | **(YENİ)** Kasa/cari negatif açılış ve kredili mevduat (KMH) istisnası (S-3f §12.3) | DALGA 3 öncesi | P-13 mevcut tutucu davranışı korur; **plan tek taraflı "reddedilir" hükmü koymaz** |
| K-25 | **(YENİ)** Gider pusulasında stopaj oranı + 7 günlük düzenleme süresi ve mevcut boş adresli kayıtların geriye dönük durumu (S-3f §12.1) | DALGA 3 öncesi | P-13 ekranı kullanıcıyı geçmiş kayıtları doldurmaya **zorlamaz** |
| K-26 | **(YENİ)** Teslimat paketinde uygulama adresinin kısayola gömülmesi (S-3f §12.4) | P-33 öncesi | Kılavuzun ilgili bölümü **koşullu** yazılır |

---

## § 5 — KARARLAR (ekip oyuyla alınacaklar)

| Karar | İçerik | Nerede kapanır | Verilen karar |
|---|---|---|---|
| R-1 | F-1 abonelik + F-3 plan kapısı sürümü | P-27 | **KOŞULSUZ 1.0.** Kurul mührü md.5 zaten 1.1'den çekmişti; `AdminModulu.cs:161-169` ayrıca bir **güvenlik borcu** |
| R-2 | F-4 telemetri sürümü | P-28 | **KOŞULSUZ 1.0.** Kurul md.3 + F-8 FAIL-CLOSED 1; ölçüm olmadan rakam ilan edilemez |
| R-3 | S-1 ticari paket sürümü | P-29 | **1.0, belge hattında PARALEL** |
| R-4 | DEMO-1 | P-30 | **1.0.** QA fikstüründen türetilmez |
| R-5 | T-4b destek paketi | P-31 | **1.0** |
| R-6 | k6 yük testi | P-25 | **1.0, B-8'den SONRA** |
| R-7 | Pilot destek dakikası ölçümü | P-32 | **1.0.** 1.0'ı bloklamaz ama ücretli tarihin ön şartıdır |
| R-8 | S-1 konumlandırması | P-29 içinde | **Toplam sahip olma maliyetine dayanır; tek rakamlı "silme oranı" İLAN EDİLMEZ** |
| R-9 | Sürüm mührü | P-01 (açılış) + P-35 (kapanış) | Planın ön koşulu |
| R-10 | SÖZLEŞME-1 | P-03 | **1.0.** Tavan yalnız azalır; kapsam `Pages+Bilesenler+Layout` |
| R-11 | Paket 5 etiket/belge şablon motoru | P-01 | Ekip oyuyla mühürlenir; karar gelmeden yalnız sabit şablon yazılır (P-17) |
| R-12 | Paket 39 has altın defteri dökümü | P-01 | Ekip oyuyla mühürlenir |
| R-13 | Paket 43 tam mobil uyarlama (**oy 2-2**) | P-01 | Kurul oylamasıyla kırılır; kırılmazsa **Tamer Bey mühürler**. Kırılmadan D-11 ve P-18 sıraya giremez |
| R-14 | UX-2-C sürümü | P-01 | Ekip oyuyla; kapsam `MIMARI-KALAN-PAKETLER`den okunur (**doğrulanmalı**) |
| R-15 | § 10 sıra 0 mührü | P-01 | **KABUL:** sadeleştirme (Y-5/Y-9/Y-29) yeni modülden ÖNCE. DALGA 4 bu mühre göre dizildi |
| R-16 | 34-Ö paket-aile eşlemesi | P-00 | `MIMARI-KALAN-PAKETLER` ile **satır satır doğrulanır**; "tahmindir" etiketi kalkar |
| R-17 | Ödeme kalemi tablosu | P-23 | **EVET**, vezne deftere yazmama kuralı korunarak |
| R-18 | Mahsup | P-23 | Ya **TÜM** ödeme yollarını kapsar ya **hiç** girmez; kısmi uygulama **mimari VETO** |
| R-19 | Layaway | P-01 | Ekip oyuyla mühürlenir |

### Planın kendi kararını geri çektiği iki nokta (Anayasa md.1)
- **Y-25 KARARI DEĞİŞTİ:** fatura ↔ hareket bağı kimlik düzeyinde **zaten var** (`FaturaKalemi.cs:34`, `FaturaModulu.cs:658`, `Entities.cs:851`). Yeni `fatura_id` kolonu **açılmaz**; iş, tek okuma yolu + `fatura_no` serbest metninin emekliye ayrılmasıdır. İlişkinin yönü ADR'ye yazılmadan migration açılmaz.
- **B-17 KARARI GERİ ÇEKİLDİ:** taslağın "negatif açılış reddedilir" mutlak hükmü kaldırıldı. K-24 gelene dek mevcut tutucu davranış korunur ve ekranda gerekçe yazılır. **Kurucuya açık sorulmuş bir kararı plan tek taraflı mühürlemez.**
- **Şube süzgeci sayacı İKİYE AYRILDI:** `GUVENLIK_ELLE_IZOLASYON` (yetki amaçlı) → 0'a iner; `KULLANICI_SUZGEC` (parametreli rapor süzgeci) **dokunulmaz**. Taslağın "51 → 0" hedefi **geçersizdir**; bugün ölçülen **63** noktanın sınıflandırma tablosu P-04'ün ilk DoD maddesidir.

---

## § 6 — DONDURULMUŞ / 1.1

| Kimlik | İçerik | Not |
|---|---|---|
| D-1 | Sadakat, RFID akışı, seri no/bandrol, teklif-proforma, hediye çeki, istek listesi, marka-koleksiyon, e-ticaret | Seri no/bandrol dilimi **K-14 ile çelişir**; çelişki K-14'ün altına yazıldı |
| D-2 | Personel prim / hakediş modülü | P-08'in `personel_id = 'SATAN'` kilidi 1.0'da kurulur, prim **hesabı** 1.1'de |
| D-3 | Döviz alım-satım (sarraf) ekranı | Lisans engeli (32 Sayılı Karar) **doğrulanmalı** |
| D-4 | Ekspertiz / değerleme fişi | Hurda alımı ve emanet-rehin işinde kullanılıyor |
| D-5 | Fiyat kilitleme (süreli fiyat sabitleme) | Fiyat listesi + kampanya var, kilit alanı yok |
| D-6 | Sertifika kartı basımı (pırlanta 4C) | PVC kart yazıcısı K-21'e bağlı olabilir (**doğrulanmalı**) |
| D-7 | Müşteri segmentasyonu | Liste üretimi bizde, **mesaj gönderimi K-15'e bağlı** |
| **D-8** | Ürün sayfası anlatımı "8 acı nokta → çözümü" | **1.0'A ALINDI** — P-29 içinde (S-1 belge hattı) |
| D-9 | Paket 36 — Patron paneli | MÜHÜR 5 § 1.1 kuyruğu |
| D-10 | Paket 37 — Pozisyon / net varlık ekranı | MÜHÜR 5 § 1.1 kuyruğu |
| D-11 | Paket 44 + PWA-1 kalanı (çevrimdışı / uygulama kabuğu) | **R-13 kararı kırılmadan başlayamaz** |
| D-12 | UX-3-C basit/profesyonel mod derinleştirmesi | UX-3-B asgarisi 1.0'da, P-14 içinde |
| D-13 | F-2 Faz B — `SistemKiracilar.razor` | MÜHÜR 4 K5 ile dalga 6'ya alınmıştı |
| Y-22 | Emanet parça kimliği (kalıcı Emanet tablosu) | **K-12 hukuki görüşü gelmeden AÇILMAZ.** P-21/P-23 tasarımı bunu engellemeyecek biçimde yapılır |
| Y-24 | Müşteri taksit planı tablosu | P-23 ödeme kalemi tablosunun Faz B'sidir; şema 1.0'da açılmaz ama **yer bırakılır** |
| Y-38 | Set birleştirme / bozma | Bugün kodda karşılığı yok, perakende akışını bloklamıyor |

---

## § 7 — DOSYA ÇAKIŞMA HARİTASI (bağlayıcı)

### Ölçüm notu (2026-09-18, `find` / `ls` / `grep` ile doğrulandı)
`Pages/`'te **42 .razor** ve **42 `^@page`** vardır ("47" yanlıştı; 47 rakamı Pages'teki **5 `.mutorj` artığının** grep'e karışmasından geliyor). `Pages` altında **alt dizin YOKTUR** (`Pages/Raporlar/` yok — gerçek dosyalar `KasaRaporu.razor`, `Raporlar.razor`, `KarZarar.razor`, `SatisAnalizi.razor`). `Bilesenler` **27**, `Layout` **5** dosya.

### Ad düzeltmeleri
`Pages/Kasalar.razor` (Kasa.razor değil) · `Pages/UrunAgaci.razor` (UrunAgac.razor değil) · `Modules/Urun/BelgeKapisi.cs` (`Modules/Belge/` yok) · `ham/surum.json` (09-uygulama-kayitlari kökünde değil) · `dokumanlar/09-uygulama-kayitlari/betikler/` (kökte `betikler` dizini yok).

### MEVCUT sayılanlar (YENİ değil)
`Bilesenler/SatirIciDuzenle.razor` (S-3c mührü — P-06'da **genişletilir**) · `dotnet/yetim-tara.sh` (P-34'te **genişletilir**) · `kurulum.sh` / `kurulum.ps1` / `yayin-al.sh` / `yedek-al-ve-dogrula.sh` / `yedek-ortak.sh` (P-33 bunları **kullanır**) · `docs/RUNBOOK-PILOT-DAVET.md` (P-32'nin temeli).

### TEK EL zincirleri (aynı anda iki paket açılamaz)

| Dosya | Sıra |
|---|---|
| `Pages/Islemler.razor` | P-15 (dokunmaz) → **P-16** → **P-18** |
| `Data/Entities.cs` | P-05 → P-08 → P-16 → P-19 → P-21 → P-23 → P-26 → P-34 |
| `Data/KuyumcuDbContext.cs` | P-04 → P-05 → P-08 → P-34 |
| `Data/HareketFabrikasi.cs` | P-08 → P-19 → P-20 → P-21 → P-23 |
| `Modules/Auth/` | Tek paket P-09 içinde **seri** (önce MFA sertleştirme, sonra 401 hizalaması) |
| `Kuyumcu.Web/Servisler/` | Yalnız P-07 (ApiIstemci, OturumServisi, ArayuzModuServisi) |
| `betikler/qa-turu.js` + `hazirlik-kurulu.js` | P-02 → P-18 (mobil sayacı) → P-24 (getBBox) |
| `betikler/yayinla.sh` + `qa-sunucu.sh` | P-02 ve P-33 |
| `docs/` + kılavuz | P-33 tek eldir; P-14, P-18, P-09'dan **sonra**. P-29 ayrı dosyalara yazar, paralel koşabilir |

**Makine kapısı (Bertan K5):** `kapi.sh` bu zincirlerde "aynı anda iki paket açık" durumunu **reddeder** (kilit dosyası + P-xx damgası). `TEK_EL_IHLAL = 0` sayacı bir kez bilerek kırılıp FAIL verdiği kanıtlanır.

### denetim.sh KURALI DEĞİŞTİ (hasım bulgusu R5)
"Tek el" yerine: **her paket YALNIZ kendi işaretli sayaç bloğunu ekler.** Blok sınırı `# >>> P-xx` / `# <<< P-xx` yorumlarıyla çizilir; her dalga sonunda **tek bir sayaç birleştirme adımı** koşar. Böylece P-06…P-34'ün sayaçları yazılabilir hâle gelir. `denetim.sh` seri sırası **P-01 ile başlar** (Bertan K8 — taslakta P-02 ile başlıyordu ama P-01 kendi sayacını ilan ediyordu).

### Kuyumcu.Tests
**HER paketin dosya listesindedir** (hasım bulgusu: taslakta yalnız bir pakette vardı). P-03, P-04, P-08 toplu sözleşme değişikliklerinin **test uyarlama payı** tahminlere dâhildir.

### Migrations
Aynı dalgada birden fazla paket migration üretebilir ama **seri** üretir. Bugün **107 .cs** ve **8 adet 0 baytlık** .cs vardır (ölçüldü); 0 baytlık sayı **artamaz**.

---

## § 8 — RİSK KÜTÜĞÜ

| No | Ciddiyet | Risk | Azaltma |
|---|---|---|---|
| R1 | KRİTİK | Şube global filtresi **fail-open** ya da **fail-blind** kurulabilir | P-04'te 63 noktanın sınıflandırma tablosu + `SubeId=null` jeton davranışının test kilidi + negatif erişim testi (gerçek HTTP) + kırılan test sayısının ölçümü (susturma yasak). **Şube bağlamı P-04'ün içine alındı** (taslakta 3 dalga sonraya kalmıştı) |
| R2 | KRİTİK | P-23 ödeme kalemi geri doldurması bakiye + VUK tevsik + MASAK'ı **birden** etkiliyor | Salt okunur etki ölçümü, belirsiz satır "BİLİNMİYOR", storno motorunun ödeme kalemlerini de döndürmesi, öncesi/sonrası yedek + **geri dönüş provası kapıya bağlı** |
| R3 | KRİTİK | Kapsam açık uçlu: tur 5 tek turda ≈59 yeni bulgu üretti | Y-42 triyaj kuralı (24 saat + `surum.json` damgası; damgasız bulgu kapı kırar) + P-00 eşleme tablosu + **kapsam freni** (ardışık iki dalgada yeni kimlik düşmüyorsa kapsam kurulu) |
| R4 | YÜKSEK | Bayat taban: taslağın 49/51/47 rakamları bugün tutmuyor | Tüm cırcır tabanları P-02'de **taze derlemede** ölçülür, ölçüm **komutu** `denetim.sh`'ta yazılı, `-ge` yerine `-eq`/azalan |
| R5 | YÜKSEK | Sayaç yazılamazlığı: `denetim.sh` "tek el" ilan edilmişti, 13 paketin sayacı kuralı deliyordu | İşaretli blok kuralı + dalga sonu birleştirme adımı; P-18 ve P-24 için `betikler/qa-turu.js` ve `hazirlik-kurulu.js` dosya listelerine eklendi |
| R6 | YÜKSEK | Yanlış dosya yolu uygulayıcıyı saptırır ve yaz-seri kuralını geçersizleştirir | `dokunulan_dosyalar` listeleri bugün `ls`/`find` ile doğrulandı; her paket başlamadan `test -e` ile yeniden doğrulanır; mevcut kolon (`Entities.cs:108 vergi_dairesi`) migration listesinden çıkarıldı |
| R7 | YÜKSEK | P-34 FK'ları yetim satırlara takılır, silme kurucudadır (K-1) | Mevcut `yetim-tara.sh` **önce** koşar, çıktısı rapora girer; FK'lar gruplar hâlinde, her grup ayrı `Down()`; abonelik şeması (P-27) FK'dan önce bitirildi |
| R8 | ORTA | Pilotun sahibi yoktu | P-32 açıldı, mevcut `RUNBOOK-PILOT-DAVET.md` temel alındı, destek dakikası P-28 ile aynı kayda yazılıyor, haftalık ölçüm zorunlu |
| R9 | ORTA | Belge hattı seri hatta sıkışırsa takvim şişer ve kurul mührüne aykırı düşer | Yaz-seri kuralı **KOD ile sınırlandı**; P-29, P-32 metin dilimi ve kılavuz ayrı hatta paralel |
| R10 | ORTA | Mevcut varlığın "YENİ" sanılıp yeniden yazılması | Hepsi "MEVCUT — genişletilir" olarak işaretlendi; P-06'ya mevcut Enter/Esc/blur davranışının **gerileme testi** eklendi |
| R11 | ORTA | Kanıt tabanı bayat: taslak "27 sayfa / 149 bulgu / 11 kritik" çerçevesini kullanıyordu | Gerçek: tur 5 (2026-09-18) **39 rota, 193 bulgu, KRİTİK 1, 119 devralınan açık, ≈59 yeni**; bugün **107 migration .cs, 8'i 0 bayt**. Takvim her dalga sonunda yeniden ölçülür |
| R12 | ORTA | `Kuyumcu.Tests` payı unutulursa tahminler düşük çıkar | Test projesi **her** paketin dosya listesinde; P-04 için "filtre açılınca kaç test kırıldı" DoD maddesi var |
| R13 | ORTA | `SAYFA_RECORD` tavanı bugün **dolu** (129/129) ve oyunlanabilirdi | Kapsam `Pages+Bilesenler+Layout`a genişletildi; her ekran paketine `BU_PAKETTE_EKLENEN_RECORD = 0` alt sayacı kondu |
| R14 | DÜŞÜK | B-16 kalıntısı (MANUAL kur ₺6.820,50) yeni ölçümleri kirletir | P-11'de temizlenir/geçersiz kılınır; her dalga raporunda **"geri alınamayan değişiklik"** bölümü zorunlu |
| **R15** | KOŞUL | K-20 iCloud eşitlemesi açıkken tek derleme **30 dk** ölçülmüştü | Tüm ajan-dakika tahminleri eşitleme **kapalıyken** geçerlidir. **`ICLOUD_ESITLEME_ACIK` sayacı** P-02'de kurulur; açıkken kapı UYARI verir ve süre ölçümleri "geçersiz taban" damgası alır |
| **R16** | YÜKSEK | **(YENİ — Eray)** TİCARİ KİLİT: K-16 (fiyat ilanı) ve K-19 (dış gönderim imzası) gelmezse satış hattı **0 gelir** üretir | Ürün teknik olarak tamamlanır ve pilot hattı çalışır; kalem § 4'te "en son" olarak işaretlendi. Bu, satış tarafının **tek durdurucusudur** |
| **R17** | YÜKSEK | **(YENİ — Mustafa)** Bütçe aritmetiği tutmuyordu: taslak hem "%40 pay dâhil" hem "her kapı 2 tur" diyordu; gerçekte **1 tur** bütçelenmişti | Düzeltildi: toplam **303 → 362,1 ajan-saat**. Formül § 9'a yazıldı; her dalga sonunda "tahmin / gerçekleşen / katsayı" satırı zorunlu |
| **R18** | ORTA | **(YENİ — bu oturumda ölçüldü)** `Pages/` altındaki 5 `.mutorj` artığı grep tabanlı sayaçları kirletiyor (rota sayısını 42 yerine 47 gösteriyor) | P-02'de tüm grepler `--include='*.razor'` kullanır; `MUTORJ_ARTIK` sayacı kurulur. **Silme K-4 ile kurucudadır** (hook `rm`'i bloklar) |
| **R19** | ORTA | **(YENİ — Mustafa)** Parasal maliyet satırı yok; kendi geliştirme maliyetimizi bilmeden K-16 fiyat zemini kurulamaz | F-8 belgesine **"GELİŞTİRME MALİYETİ"** bölümü eklenir; ajan-saat → token → ₺ dönüşümü D0+D1 üzerinden kalibre edilir, sapma %25'i aşarsa kurula döner |

---

## § 9 — TAKVİM

### Bütçe formülü (artık yazılı — Mustafa K-F1)

```
dalga_ajan_saat = (paket_dakikaları_toplamı / 60 × 1,4) + (QA_turu_sayısı × ölçülen_tur_süresi)
```

`ölçülen_tur_süresi` başlangıçta **8 ajan-saat** varsayılır; **P-02'de kronometreyle ölçülür** ve `ham/qa-tur-suresi.json`'a gerçek değerle yazılır. Sonraki tüm dalga bütçeleri o değerle yeniden hesaplanır.

### Dalga dağılımı (düzeltilmiş)

| Dalga | Paket dk | ×1,4 | QA turu | Toplam ajan-saat | Taslakta |
|---|---|---|---|---|---|
| D0 — İzlenebilirlik | 165 | 3,9 | 0 (kod dokunulmaz) | **3,9** | 5,3 |
| D1 — Mekanizma zemini | 870 | 20,3 | 2 × 8 | **36,3** | 28,7 |
| D2 — Mekanizma II | 930 | 21,7 | 2 × 8 | **37,7** | 30,1 |
| D3 — Ucuz kablolar | 960 | 22,4 | 2 × 8 | **38,4** | 30,8 |
| D4 — Tezgâh katmanı | 1680 | 39,2 | 2 × 8 | **55,2** | 47,6 |
| D5 — Çıkış kapıları | 1140 | 26,6 | 2 × 8 | **42,6** | 35,0 |
| D6 — Büyük yapısal | 1800 | 42,0 | 2 × 8 | **58,0** | 50,4 |
| D7 — Ticari ayak | 1350 | 31,5 | 2 × 8 | **47,5** | 39,9 |
| D8 — FK ve kapanış | 1140 | 26,6 | 2 × 8 | **42,6** | 35,0 |
| **TOPLAM** | **10.035 dk** | **234,2** | **128** | **362,1** | 303 |

> **Fark nereden geldi:** taslak her kapıya **1** QA turu bütçelemişti (303 = 234 + 8×8 + pay), oysa kendi 3. ilkesi **2 tur** istiyor. Eksik: **8 dalga × 8 saat = 64 ajan-saat.** Mustafa'nın FinOps bulgusu **doğrulandı ve düzeltildi** — kopyalanmadı.

### Takvim

- **Kod hattı seridir.** Günde 6-8 verimli ajan-saat varsayımıyla: 362,1 ÷ 7 ≈ **52 iş günü** (aralık 46-61) ≈ **11-15 takvim haftası**
- **Belge / pazarlama / pilot hattı** (P-29, P-32 metin dilimi, P-33 kılavuzu) **paralel** koşar ve bu süreyi **uzatmaz**
- **Pilot penceresi** 4-6 hafta paralel yürür; DALGA 4 kapısından itibaren başlayabilir. 1.0'ı **bloklamaz**, ama "ücretli tarih" ancak pilot + iki ardışık temiz tur sonrası kurulabilir
- **Kapanış** (P-35) +1 hafta
- **Sonuç: satışa hazır ilanı ≈ 12-16 takvim haftası**

### Hızın kaynağı (dürüstlük şartı)
"Normal paket 35-60 dk, büyük paket 60-120 dk" rakamları **dalga 2b döneminde (1650 test, 33 migration)** ölçülmüştür. Bugün depo **107 migration .cs** ve daha büyük bir süittedir; süit süresi arttıkça paket başına doğrulama süresi de artar. Bu yüzden **takvim her dalga sonunda yeniden ölçülür** ve `ORKESTRATOR-DURUM.md`ye yazılır.

### Sapma freni (Mustafa K-F6 + Ramazan K2)
Her dalga raporunda **"tahmin X / gerçekleşen Y / katsayı Y÷X"** satırı zorunlu. Katsayı iki ardışık dalgada **≥ 1,5** çıkarsa kalan dalgaların takvimi yeniden hesaplanır ve kurucuya **tek sayfalık sapma bildirimi** yazılır (karar değil, bilgi). İlk iki dalgada kümülatif sapma **%30**'u aşarsa DALGA 4'ten sonrası kapsam olarak **yeniden oylanır**.

### Geçerlilik koşulu
**Tüm rakamlar K-20 (iCloud eşitlemesi) KAPALIYKEN geçerlidir.** Açıkken tek derleme 30 dk ölçülmüştü; o hâlde takvim **geçersizdir** ve yeniden ölçülmelidir. Bu bir varsayım değil, **açıkça yazılı bir koşuldur** ve P-02'de sayaca bağlanmıştır.

---

## § 10 — HASIM TURUNUN BULGULARI VE PLANDA NASIL KAPATILDIĞI

Üç hasım merceği koştu: **(1) Unutulan kalemler**, **(2) Sıra ve bağımlılık**, **(3) Kimlik kataloğunun izlenebilirliği**. Her üçü de **KOŞULLU** verdi. Toplam **30 bulgu**. Aşağıda her biri ve planın verdiği karşılık.

### Mercek 1 — Unutulan kalemler (14 bulgu)

| # | Bulgu | Ciddiyet | Planda nasıl kapatıldı |
|---|---|---|---|
| 1 | Varsayılan "Senaryo A" ürünü **hiçbir zaman satılamaz** bırakıyordu (kurul md.2/3 ile çelişki) | KRİTİK | **KABUL.** Senaryo A kaldırıldı; F-1/F-3 ve F-4 **koşulsuz 1.0** (P-27, P-28). Kurucuya kalan tek kalem K-16 fiyat ilanı (§ 3) |
| 2 | Kurulun "**iki ardışık temiz tur**" ölçütü planın hiçbir kapısında yoktu | KRİTİK | **KABUL.** İlke 3 oldu; her dalga kapısına yazıldı ve **+64 ajan-saat** takvime eklendi (§ 9) |
| 3 | B-50/B-51 açılmamış kutu: tur 5'te 193 bulgu var, envanter 54 B kimliğine sıkıştırıyor | KRİTİK | **KABUL.** P-00 mühürden **önce** koşar; tur 4+5 ve S-3e/S-3f satır satır açılır, eşleme tablosu belgeye girer |
| 4 | Cırcır tabanı bayat: düzenlenemeyen giriş gerçekte 32, plan 49'dan başlatıyordu | YÜKSEK | **KABUL.** "49" plandan **çıkarıldı**; taban P-02'de taze derlemede ölçülür (İlke 2 + P-06 sayacı) |
| 5 | Envanterde ve K listesinde olmayan 4 kurucu kararı var; plan birini **tek taraflı mühürlüyor** | YÜKSEK | **KABUL.** K-23…K-26 açıldı (§ 4). B-17'nin "negatif açılış reddedilir" hükmü **geri çekildi** (§ 5) |
| 6 | Y-25'in `fatura_id` migration'ı **çürütülmüş öncüle** dayanıyor (doğrulama verdicti YANLIS) | YÜKSEK | **KABUL.** Kolon **açılmaz**; iş tek okuma yolu + serbest metnin emekliye ayrılması. İlişki yönü **ADR'ye** bağlandı (P-08) |
| 7 | Pilotun sahibi yok; kurul "bugün verilebilecek tek ticari biçim ücretsiz pilottur" diyor | YÜKSEK | **KABUL.** **P-32 PİLOT-1** açıldı; mevcut `RUNBOOK-PILOT-DAVET.md` temel alındı |
| 8 | Var olan iki dosya "(YENİ)" işaretli: `SatirIciDuzenle.razor`, `yetim-tara.sh` | ORTA | **KABUL + doğrulandı** (bugün `test -e`). İkisi de "MEVCUT — genişletilir"; P-06'ya gerileme testi, P-34'e "önce mevcut betiği koş" maddesi eklendi |
| 9 | Dosya yolları ve dosya sayısı hatalı (47 razor, Kasa.razor, Pages/Raporlar/, UrunAgac, Modules/Belge, surum.json, betikler/) | ORTA | **KABUL + ölçüldü.** Hepsi düzeltildi (§ 7). **Ek olarak 47'nin kaynağı bulundu:** 5 `.mutorj` artığı (R18) |
| 10 | Yeni bulgu **triyaj kuralı yok**: 8 dalga × QA turu yeni bulgu üretecek | ORTA | **KABUL.** Y-42 triyaj kuralı **ilke** oldu (İlke 5) + P-00 DoD + kapsam freni |
| 11 | Kanıt tabanı iki tur bayat; hız ve pay tahminleri dalga 2b dönemine ait | ORTA | **KABUL.** R11 risk kütüğüne yazıldı; § 9'da hızın hangi dönemden geldiği açıkça belirtildi, her dalga sonunda yeniden ölçüm zorunlu |
| 12 | Kurul "belge hattı kod hattıyla **PARALEL**" dedi; plan katı tek hat kuruyordu | ORTA | **KABUL.** Yaz-seri kuralı **koda daraltıldı** (İlke 7); P-29/P-32/P-33 ayrı hatta |
| 13 | Tur 5'in ölçtüğü **istek fırtınası + 401** hiçbir kimliğe bağlı değil | ORTA | **KABUL.** **B-57** açıldı, P-07'ye kondu, `ISTEK_TEKRAR_TAVANI` sayacı yazıldı |
| 14 | **KDV 23/e** özel matrahın varsayılan kapalı anahtarı sahipsiz | ORTA | **KABUL.** **Y-40** açıldı, P-26'ya kondu; K-10 gelmezse ürün içinde "HARİÇ" beyanı |

### Mercek 2 — Sıra ve bağımlılık (12 bulgu, 12 salt okunur araç çağrısıyla ölçüldü)

| # | Bulgu | Ciddiyet | Planda nasıl kapatıldı |
|---|---|---|---|
| 15 | "ELLE_SUBE_SUZGEC 51 → 0" ne mümkün ne doğru: 51'in çoğu **kullanıcı seçimli rapor süzgeci** | KRİTİK | **KABUL.** Sayaç **ikiye ayrıldı**; taban bugün **63** ölçüldü; P-04'ün ilk DoD maddesi sınıflandırma tablosu |
| 16 | Şube **bağlamı** P-04'te yok, 3 dalga sonra geliyordu → filtre dayanaksız kurulur | KRİTİK | **KABUL.** Şube bağlamı + `SubeId=null` sözleşmesi **P-04'ün içine** alındı |
| 17 | P-09'un `HttpClient` sayacı kendi dosya listesiyle kapanamaz (4 tüketici var) | YÜKSEK | **KABUL + ölçüldü.** `OturumServisi`, `ArayuzModuServisi`, `BasitSatisAkisi.razor` P-07 listesine eklendi |
| 18 | `window.print` sayacı pakete dâhil olmayan üç dosyada; biri farklı çıktı ailesi | YÜKSEK | **KABUL + ölçüldü** (PusulaBelgesi:213, IslemDefteri:307, UrunEtiketleri:263). `UrunEtiketleri` **termal aile** olarak beyaz listeye alındı; "3 → 0" hedefi kaldırıldı |
| 19 | P-01 kendi sayacını yazacağı dosyaya dokunmuyor; `surum.json` yok sanılıyordu | YÜKSEK | **KISMEN KABUL.** `denetim.sh` P-01 listesine eklendi ve seri sıra P-01 ile başlatıldı. **DÜZELTME:** `ham/surum.json` **VARDIR** (bugün ölçüldü: 6.453 bayt, 16 Eylül) — hasım yanlış yola bakmış; mevcut mühür mekanizmasıyla ilişkisi P-01 DoD'una yazıldı |
| 20 | 13 paketin sayacı `denetim.sh` "tek el" kuralını deliyordu; P-20 ayrıca betik kuralını ihlal ediyordu | YÜKSEK | **KABUL.** İşaretli blok kuralı + dalga sonu birleştirme (§ 7); `qa-turu.js` / `hazirlik-kurulu.js` P-18 ve P-24 listelerine eklendi |
| 21 | "FK en son" ilkesi abonelik tabloları tarafından deliniyordu | ORTA | **KABUL.** P-27 şema dilimi FK'dan **önce** biter ve P-34 önkoşuluna eklendi |
| 22 | `yetim-tara.sh` zaten var, plan "(YENİ)" sayıyordu | ORTA | **KABUL** (bkz. #8) |
| 23 | P-25'in "satıcı bazlı" raporu kodun kendi notuna göre P-26'ya bağlı — sıra ters | ORTA | **KABUL.** Rapor adı **"KAYDEDEN bazlı"** olarak sabitlendi; "satıcı bazlı" iddiası P-08'in `personel_id='SATAN'` kilidine bağlandı |
| 24 | `SAYFA_RECORD` tavanı oyunlanabilir ve bugün **sıfır boşluk** var (129/129) | ORTA | **KABUL + ölçüldü** (`denetim.sh:310` = 129; ham grep 157). Kapsam `Pages+Bilesenler+Layout`a genişletildi + paket başına alt sayaç |
| 25 | Bazı dosya yolları depoda başka yerde (`Modules/Belge`, `betikler/`, `vergi_dairesi` zaten var) | DÜŞÜK | **KABUL + ölçüldü.** Düzeltildi; `test -e` kapısı ilke oldu (İlke 12) |
| 26 | `Kuyumcu.Tests` hiçbir paketin dosya listesinde yok | ORTA | **KABUL.** Her pakete eklendi; P-04'e "kaç test kırıldı" DoD maddesi |

### Mercek 3 — Kimlik kataloğunun izlenebilirliği (rapor kesildi, ilk bulgu tam)

| # | Bulgu | Ciddiyet | Planda nasıl kapatıldı |
|---|---|---|---|
| 27 | **147 kimliklik katalog hiçbir depo dosyasında yok.** `grep -rlE '\bY-[0-9]+\b'` tüm ByGatech ağacında **sıfır** sonuç. `risk-kaydi.md`'deki R-01…R-28 **başka bir şeydir** (PMBOK risk maddeleri); `teknik-borc-envanteri.md` kendi TB-xx numaralamasını kullanıyor; bugünkü kıyas raporunda B-/Y-/R- deseni **sıfır** | KRİTİK | **KABUL — planın en değerli düzeltmesi.** `KAPSAM-ENVANTERI.md` (P-00) her kimliği kaynağına bağlar; kaynağı bulunamayan kimlik **"bu oturumda sentezlendi"** diye işaretlenir. **Bu belge olmadan plan mühürlenmez ve hiçbir dalga başlamaz** (İlke 6) |

> **Mercek 3'ün raporu çıktı sınırında kesildi.** Anayasa md.1 gereği bunu gizlemiyorum: bu merceğin **karar alanı okunamadı** ve kalan bulguları alınamadı. P-00'ın DoD'una *"mercek 3'ün tamamlanmamış turu yeniden koşulur ve çıktısı envantere eklenir"* maddesi eklendi.

### Hasım turunun "unutulan kimlikler" listesinden doğan **yeni kimlikler**

| Yeni | Kaynak | Nereye gitti |
|---|---|---|
| B-55 | S-3f §11.4 POS tahsilat varken "yeni kasaya taşı" göç akışı | P-22 |
| B-56 | S-3f §11.3 POS kasa değişiminin tahsilatlı reddi 5198'de canlı denenmedi | P-22 |
| B-57 | QA tur 5 konsol kanıtı: `kur/guncel` 130+ tekrar + arada 401 | P-07 |
| B-58 | QA tur 4/5'in 119 açık + ≈59 yeni bulgusunun kimliklendirilmesi | P-00 |
| Y-40 | KDV 23/e özel matrah varsayılanı (motor + kolon hazır, sahipsizdi) | P-26 |
| Y-41 | Ücretsiz pilot paketi: davet, kısıt beyanı, destek dakikası | P-32 |
| Y-42 | Yeni bulgu kabul/triyaj kuralı | P-00 + P-35 |
| K-23…K-26 | S-3f §12.1-12.4 kurucu kararları | § 4 |
| **R16, R17, R18, R19** | Eray, Mustafa (×2), bu oturumun ölçümü | § 8 risk kütüğü |

**Sonuç: 147 → 158 kimlik.** Hasım turunun tespit ettiği **hiçbir bulgu reddedilmedi**; biri (#19, `surum.json` yokluğu) **kısmen düzeltildi** çünkü ölçüm hasmı yanlış çıkardı.

---

## § 11 — OYLAMA TABLOSU VE MÜHÜR

### Oylar (7 mercek)

| Mercek | Kişi | Oy | Ana itiraz |
|---|---|---|---|
| Mimari bütünlük ve sıralama | **Bertan** (02 Baş Mimar) | KOŞULLU | Tek-el kuralının **makine kapısı yok**; P-04 fail-open riski `denetim.sh`'ta görünmez; P-23 geri dönüş provası kapıya bağlı değil |
| Güvenlik / Veto | **Mehmet** (06 Güvenlik) | KOŞULLU | MFA vetosu **canlı kanıt** olmadan kalkmaz; şube filtresi, telemetri PII ve destek paketi sır taşımama iddiası **beyanla değil denetimle** kanıtlanmalı |
| QA / Test | **Aşkın** (04 QA) | KOŞULLU | Plan "DoD yazıldı" seviyesinde; `Down()` **koşturulmalı**, mobil parite **gerçek tarayıcıda** ölçülmeli, ham komut çıktısı saklanmalı |
| Teslimat / Müşteri İlişkileri | **Köprü** (07 Teslimat) | KOŞULLU | Kısıt beyanı **dört ayrı yerde**; **göç (rakip programdan veri alma) planda hiç yok**; pilot hattının başlangıcı belirsiz |
| Strateji / Proje Yönetimi | **Ramazan** (01 Strateji) | KOŞULLU | Kapsam büyümesine karşı **ölçülebilir fren yok**; `PlanGuncelle` güvenlik açığı DALGA 7'ye ertelenmiş; kurucu kalemlerinin **ihtiyaç tarihi** yok |
| FinOps | **Mustafa** (09 FinOps) | KOŞULLU | Bütçe **üç dalgada aritmetik tutmuyor**; "insan-gün" tanımsız birim; **parasal maliyet sıfır satır**; K-20 koşulu ölçülmüyor |
| Satış ve Pazarlama | **Eray** (08 Satış) | KOŞULLU | Demo provası **tek koşumla** kanıt sayılıyor; pilot metni takvimin sonuna sıkışmış; **K-16/K-19 ticari kilidi** risk kütüğünde yok |

**Sayım: EVET 0 · KOŞULLU 7 · HAYIR 0.**
**Mehmet (veto yetkisi) HAYIR vermedi — KOŞULLU verdi.**

### MÜHÜR

> # MÜHÜRLENDİ
>
> **Mühür kuralı:** "MÜHÜRLENDİ" yalnız **Mehmet dâhil hiçbir HAYIR oyu yoksa** kurulabilir. Yedi merceğin hiçbiri HAYIR vermedi. Mehmet'in oyu **KOŞULLU**'dur ve koşulları aşağıda **bağlayıcı** olarak plana girmiştir.
>
> **Mühürleyen:** Tamer Bey · **Tarih:** 2026-09-18
> **Kapsam:** PLAN v2 — 9 dalga, 36 paket, 158 kimlik, 362,1 ajan-saat, 11-15 takvim haftası.

### BAĞLI KOŞULLAR (50 madde — hepsi bağlayıcıdır)

Her koşul, ilgili paketin **DoD'una satır olarak** girer. Koşulu sağlanmayan paket **kapanmaz**; koşulu sağlanmayan dalga **kapısını açmaz**.

#### Bertan (Mimari) — BK-01 … BK-08
| No | Koşul | Bağlandığı paket |
|---|---|---|
| BK-01 | P-03 (Contracts) P-06/P-10/P-13/P-16'dan önce **tamamen** biter. Ölçüt: `CONTRACTS_DISI_MODEL=0` ve `SAYFA_RECORD_TAVAN ≤ 60` yeşil; aksi hâlde DALGA 2 kapısı açılmaz | P-03 |
| BK-02 | 63 `SubeId ==` noktasının (a/b/c/d) sınıflandırma tablosu **kod değişiminden önce** yayımlanır. Ölçüt: sınıfsız satır 0, `GUVENLIK_ELLE_IZOLASYON=0`, `IGNORE_QUERY_FILTERS ≤ 4`, `SubeId=null` fail-closed negatif test 2 adet yeşil, kırılan test sayısı sayıyla yazılı, susturulan test 0 | P-04 |
| BK-03 | P-23 geri doldurma migration'ı ancak **(i)** salt okunur etki raporu **(ii)** `Down()` tatbikat çıktısı **(iii)** yedek + geri dönüş provası kaydı üçü de rapora girdikten sonra koşar. R-18 kısmi uygulanırsa paket **reddedilir** | P-23 |
| BK-04 | Y-25 için ilişkinin yönü (1 fatura ↔ N hareket) **ADR olarak** yazılır (`dokumanlar/02-mimari/ADR-00xx`). ADR yoksa hiçbir bağ migration'ı açılmaz | P-08 |
| BK-05 | Tek-el kuralları **makineyle** doğrulanır: `kapi.sh` "aynı anda iki paket açık" durumunu reddeder. `TEK_EL_IHLAL=0` sayacı bir kez bilerek kırılıp FAIL verdiği kanıtlanır | P-02 |
| BK-06 | P-34 önkoşulu sertleşir: `yetim-tara.sh` **güncel** çıktısında toplam yetim = 0 **veya** her grup için K-1 yazılı. Her FK grubu ayrı migration + koşturulmuş `Down()`. Tek dev migration **reddedilir** | P-34 |
| BK-07 | Her cırcır sayacının **ölçüm komutu** `denetim.sh` içinde yorum olarak yazılı; hiçbir taban belgeden kopyalanmaz. Tabanlar `ham/olcum-<tarih>.json` olarak sürümlenir; kopyalanan taban sayısı 0 | P-02 |
| BK-08 | `denetim.sh` işaretli blok kuralı DALGA 1'de devreye girer, dalga sonu birleştirme **betikle** koşar. Bloksuz eklenen sayaç 0; birleştirme sonrası tek koşumda TEMİZ | P-02, tüm dalga sonları |

#### Mehmet (Güvenlik, veto yetkisi) — BK-09 … BK-17
| No | Koşul | Bağlandığı paket |
|---|---|---|
| BK-09 | Sınıflandırma tablosu yayımlanmadan ve negatif erişim testi (A şubesi jetonuyla B şubesi kaydı → 404) **yeşil** olmadan P-04 kapanmaz; `IgnoreQueryFilters` sayısı **artamaz** | P-04 |
| BK-10 | **MFA vetosu**, `AuthModulu.cs:244` ve `:431` aynı generic 401'i dönene, yetki matrisi otomatik üretilip özniteliksiz uç negatif testiyle kırılana ve MFA uçtan uca **canlı** kanıtlanana kadar **KALDIRILMAZ** | P-09 |
| BK-11 | P-23 geri doldurması öncesi/sonrası **tam DB yedeği + koşturulmuş geri dönüş provası** raporda olmadan başlamaz; belirsiz satır "BİLİNMİYOR" işaretlenir | P-23 |
| BK-12 | `TELEMETRIDE_KISISEL_VERI=0` **canlı ölçümle** kanıtlanmadan hiçbir telemetri verisi toplanmaz | P-28 |
| BK-13 | `AdminModulu.cs:161-169` yetkisiz `PlanGuncelle` kapanmadan ve `PLAN_DEGISIMI_IZSIZ=0` canlı denetim izi kanıtı olmadan abonelik modülü 1.0'a alınmaz | P-09 (dilim), P-27 |
| BK-14 | **K-5 sır rotasyonu** yayın öncesi tamamlanır; tamamlanmadan P-02 yayın damgası **Production ortamına açılmaz** | P-02 (K-5 bağımlı) |
| BK-15 | P-34'ten önce `yetim-tara.sh` çıktısı raporlanır; yetim varken **hiçbir** FK grubu kurulmaz; **cascade YASAK**, yalnız RESTRICT | P-34 |
| BK-16 | **K-1** kapanmadan P-30 demo kiracısı ve P-34 FK grubu başlamaz | P-30, P-34 |
| BK-17 | `TANILAMA_PAKETINDE_SIR=0` ve `IC_KOD_SIZINTISI=0` QA'dan **bağımsız olarak Mehmet'in turunda da** doğrulanmadan `/destek` kanalı açılmaz | P-31 |

#### Aşkın (QA) — BK-18 … BK-25
| No | Koşul | Bağlandığı paket |
|---|---|---|
| BK-18 | `KAPSAM-ENVANTERI.md` yayımlanmadan **hiçbir dalga başlamaz**; kaynağı bulunamayan kimlik "bu oturumda sentezlendi" diye işaretli olmalı, aksi hâlde oy **HAYIR'a döner** | P-00 |
| BK-19 | Her dalga kapısı kritik=0 **ve** düzenlenemeyen=0 şartını **iki ardışık turda** (ikincisi kod değişikliği olmadan) sağlar; her dalga raporunda tarih + tur numarasıyla kanıtlanır | Tüm dalga sonları |
| BK-20 | P-04 negatif testi **gerçek HTTP isteğiyle** canlı doğrulanır; kırılan test sayısı somut rakamla verilir, susturulan test 0 | P-04 |
| BK-21 | P-05/P-19/P-21/P-23/P-26/P-34 migration'larının her biri için `Down()` **gerçekten bir kez koşturulur** ve rollback sonrası şema/veri durumu rapora eklenir. "Yazıldı" yeterli değil | P-05, P-19, P-21, P-23, P-26, P-34 |
| BK-22 | MFA akışı uçtan uca **canlı** ölçülür; kurtarma kodunun yalnız başarılı doğrulamada tüketildiği **mutasyon kanıtıyla** (`mut.sh`) gösterilir. **bUnit tek başına yeterli sayılmaz** | P-09 |
| BK-23 | Her dalga sonunda sayaç birleştirmeden sonra `denetim.sh` TEMİZ + tam süit 0 başarısız çıktısı **ham log** olarak saklanır (ekran görüntüsü/özet değil) | Tüm dalga sonları |
| BK-24 | P-18 mobil paritesi: 42/42 rotada `scrollWidth ≤ 375` **gerçek tarayıcıda tek tek** ölçülüp rapor tablosuna dökülür; **örnekleme kabul edilmez** | P-18 |
| BK-25 | Test kiracıları (`PAKET*/TEST*/QA*`) her dalga sonunda **silinir**; silinmediyse dalga kapanmaz | Tüm dalga sonları |

#### Köprü (Teslimat) — BK-26 … BK-32
| No | Koşul | Bağlandığı paket |
|---|---|---|
| BK-26 | Kısıt beyanı **tek kaynaktan** üretilir: `docs/HARIC-LISTESI.md`. P-29, P-32, ürün içi beyan ve P-33 kılavuzu bu dosyayı **gömerek** kullanır. Sayaç: `HARIC_BEYAN_KOPYA = 0` | P-29, P-32, P-33 |
| BK-27 | P-32 davet metni **DALGA 5 kapısı kapandığında hazır** olur ve kurucuya o tarihte sunulur. `PILOT-KAYIT.md`de "hazır tarihi" ve "sunuldu tarihi" dolu; boşsa DALGA 7 kapısı açılmaz | P-32 |
| BK-28 | Pilot dönüş kanalı **haftalık** ölçülür (pilot sayısı, destek kaydı, ilk yanıt süresi, kapanan/açık). **Boş hafta satırı 0**; F-8 en az **4 haftalık** gerçek veriyle dolar, tek haftadan ekstrapolasyon yok | P-32 |
| BK-29 | "İlk satışa kadar geçen süre" **iki kez** ölçülür: biri ekip üyesi, biri ürünü hiç görmemiş kişi. İkisi de tarih + süre ile yazılı; tek ölçümle rakam ilan edilmez | P-29 |
| BK-30 | `/yedekler` ekranında "son tatbikat tarihi" görünür ve **30 günden eski olamaz** (`YEDEK_TATBIKAT_BAYAT = 0`). Pilot müşteriye kurulum yapılacaksa **ayrıca kaydedilir** | P-33 |
| BK-31 | **Göç kararı yazılı olmalı:** ya CSV ile cari + ürün + açılış bakiyesi içe aktarımı 1.0'a alınır, ya `HARIC-LISTESI.md`ye "göç yoktur, veri elle girilir" yazılır. **Sessiz bırakılamaz** — P-29 mühründen önce | P-29 |
| BK-32 | `docs/RUNBOOK-TESLIMAT.md` (kurulum adımı, adres/kısayol K-26, destek kanalı, yedek anahtarı K-18) yazılmadan **P-35 sürüm mührü atılmaz** | P-33, P-35 |

#### Ramazan (Strateji/PM) — BK-33 … BK-37
| No | Koşul | Bağlandığı paket |
|---|---|---|
| BK-33 | **Kapsam freni:** her dalga kapısında yeni kimlik sayısı ölçülüp `surum.json`'a yazılır. Ardışık iki dalgada yeni kimlik bir önceki dalganın **%50'sinin altına inmiyorsa** Tamer Bey başkanlığında kapsam kurulu toplanır ve 1.0 sınırı yeniden çizilir. Sayaç: `YENI_KIMLIK_DALGA` | P-00 + tüm dalga sonları |
| BK-34 | **Takvim yeniden ölçümü zorunlu:** her dalga sonunda `SAPMA_KATSAYISI = gerçekleşen ÷ tahmin`. İki ardışık dalgada ≥ 1,5 ise kalan takvim yeniden hesaplanır ve kurucuya **tek sayfalık sapma bildirimi** yazılır (karar değil, bilgi) | Tüm dalga sonları |
| BK-35 | **P-27 dilimi öne alınır:** `AdminModulu.cs:161-169` yetkisiz `PlanGuncelle` bir **güvenlik açığıdır**; yalnız bu uç için yetki kapısı + denetim izi **DALGA 2'ye** (P-09 DoD'una ek madde) çekilir. `YETKISIZ_PLAN_YAZAN_UC = 0` DALGA 2 kapısında yeşil | P-09 |
| BK-36 | **Kurucu kalemi takvimi:** K-1, K-10, K-13, K-20 için "ihtiyaç tarihi" tablosu P-01 ile birlikte kurucuya iletilir. Her kurucu kaleminde "ihtiyaç tarihi" alanı dolu; **boş alan 0** | P-01, § 4 |
| BK-37 | **Pilot 1.0'ı beklemez:** P-32 davet metni **DALGA 3 sonunda** (gösterilebilir ürün + yazılı kısıt beyanı) hazır olur; gönderim kurucudadır. DALGA 3 kapı raporunda `PILOT-KAYIT.md` ve kısıt listesi mevcut, `PILOT_KISIT_BEYANSIZ_DAVET = 0` | P-32 |

> **Not — BK-27 ile BK-37 arasındaki gerilim:** Köprü "DALGA 5", Ramazan "DALGA 3" diyor. **Tamer Bey mührü: metin DALGA 3 sonunda hazır olur (erken hazırlık maliyetsizdir), kurucuya sunum DALGA 5 kapısında yapılır** — böylece sunulan metin gösterilebilir bir ürüne dayanır. İki koşul da bu şekilde sağlanır.

#### Mustafa (FinOps) — BK-38 … BK-44
| No | Koşul | Bağlandığı paket |
|---|---|---|
| BK-38 | Her dalganın toplam ajan-saati **formülden türetilir** ve formül plan metnindedir (§ 9). D1/D6/D8'deki 7-8 saatlik sapmalar **düzeltildi**. Sayaç: `BUTCE_MUTABAKATSIZ_DALGA = 0` | § 9 + tüm dalga sonları |
| BK-39 | **QA turu süresi ölçülür, varsayılmaz:** P-02'de bir tam tur (42 rota) kronometreyle ölçülür, `ham/qa-tur-suresi.json`'a gerçek değerle yazılır; sonraki bütçeler o değerle yeniden hesaplanır | P-02 |
| BK-40 | **"İnsan-gün" birimi kaldırıldı.** Taslağın "158 insan-günü" ile "10-12 hafta"sı çelişiyordu (tek insan var, görevi imza/karar). Plan artık **iki metrik** kullanır: ajan-saat ve takvim haftası; kurucu tarafı "gözetim günü" olarak ayrı ve küçük yazılır | § 9 |
| BK-41 | **Parasal tahmin zorunlu:** F-8 belgesine "GELİŞTİRME MALİYETİ" bölümü eklenir; ajan-saat → token → ₺ dönüşümü D0+D1 üzerinden kalibre edilir. Sapma **%25**'i aşarsa takvim ve kapsam kurula döner | P-28 (F-8) |
| BK-42 | **K-20 sayaca bağlanır:** `denetim.sh`'a `ICLOUD_ESITLEME_ACIK` kontrolü eklenir; açıkken kapı UYARI verir ve o dalganın süre ölçümleri **"geçersiz taban"** damgası alır | P-02 |
| BK-43 | Her dalga raporunda "tahmin X / gerçekleşen Y" satırı zorunlu; `ORKESTRATOR-DURUM.md`de kümülatif sapma tutulur. İlk iki dalgada kümülatif sapma **%30**'u aşarsa DALGA 4 sonrası kapsam **yeniden oylanır** (dalga 5-8 sırası dondurulur) | Tüm dalga sonları |
| BK-44 | **P-23 (600 dk) ve P-34 (900 dk)** için alt-adım kırılımı ayrı yazılır (ölçüm / migration / test / geri dönüş provası); aksi hâlde aşım görünmeden büyür | P-23, P-34 |

#### Eray (Satış) — BK-45 … BK-50
| No | Koşul | Bağlandığı paket |
|---|---|---|
| BK-45 | 15 dakikalık gösterim betiği **iki ayrı ajan tarafından iki kez** koşulur, süre `ham/demo-prova-<tarih>.json`'a yazılır; **> 20 dk ise paket kapanmaz.** Tek koşum kanıt sayılmaz | P-30 |
| BK-46 | Yeni sayaç `DEMO_ROTA_KRITIK = 0`: gösterim betiğinin dokunduğu her rota **iki ardışık turda** kritik = 0 ve boş/çelişkili özet = 0 verir | P-30 |
| BK-47 | `PILOT_KISIT_BEYANSIZ_DAVET` kapsamı **demo kiracısını da içerir**: hariç listesi yalnız belgede değil, demo kiracısının **giriş ekranında okunur** | P-30, P-32 |
| BK-48 | P-32'nin metin dilimi **DALGA 4 kapısının kontrol listesine** bağlanır; pilot ölçümü paralel hatta erken başlar ki "ücretli tarih" 16+ haftaya kaymasın | P-32 |
| BK-49 | `F8_OLCULMEMIS_SATIR = 0` sağlanmadan hiçbir satış materyali "TASLAK — ilan edilmemiştir" damgasından çıkmaz; **hiçbir demoda, hiçbir kanalda rakam telaffuz edilmez**; tek rakamlı tasarruf/silme oranı ilanı **yasaktır** | P-28, P-29, P-30 |
| BK-50 | Risk kütüğüne **R16 — TİCARİ KİLİT** eklenir: K-16 ve K-19 gelmezse satış hattı 0 gelir üretir; bu satış tarafının **tek durdurucusudur** ve yazılı görünür | § 8 |

### Mührün kapsamadığı şey (açıkça)
- **Mercek 3'ün kesilen turu.** Tam çıktısı alınamadı; P-00 DoD'una yeniden koşum maddesi eklendi. Bu mühür, o turun kalan bulgularının **çıkmayacağını iddia etmez.**
- **147 kimliğin tamamının kaynağa bağlandığı iddiası.** Bu iddia bugün **kurulamaz** — kurulması P-00'ın işidir. Mühür, planın **kapsamını** mühürler, kapsamın **eksiksizliğini** değil.

---

## § 12 — ENVANTER İZLENEBİLİRLİK TABLOSU

> **Kural:** her kimlik **tam olarak bir** yere yazılıdır. Yazılmamış kimlik yoktur. Bu tablo `KAPSAM-ENVANTERI.md`nin (P-00) omurgasıdır; P-00 bunun üstüne **kaynak** sütununu ekler.

### Eski borçlar (B-1 … B-58) — 58 kalem

| Kimlik | Dalga | Paket | Konu |
|---|---|---|---|
| B-1 | D1 | P-02 | QA ölçüm kapısı: 42/42 rota, öykünme kapalı |
| B-2 | D1 | P-02 | Yayın anlık görüntüsü + ortam/sürüm damgası |
| B-3 | D5 | P-22 | TASLAK fatura ve kalemleri düzenlenebilsin |
| B-4 | D2 | P-06 | Düzenlenemeyen veri girişi → 0, şablon kuralı |
| B-5 | D7 | P-33 | Temiz makinede sıfırdan kurulum tatbikatı |
| B-6 | D2 | P-09 | MFA uçtan uca canlı ölçüm |
| B-7 | D6 | P-24 | Türetilmiş özet ↔ liste toplamı mutabakatı |
| B-8 | D6 | P-25 | 34-Ö ölçek + kültür borcu |
| B-9 | D4 | P-18 | Mobil parite, 42/42 rotada 375 px |
| B-10 | D2 | P-07 | 429 bandı + geri çekilme + jeton yarışı |
| B-11 | D7 | P-33 | Kılavuzun tek kaynaktan yeniden yazımı |
| B-12 | D3 | P-13 | /uyum GONDERILDI kaydında düzeltme yolu |
| B-13 | D2 | P-06 | Ortak SatirIciDuzenle sözleşmesi (Enter/odak) |
| B-14 | D3 | P-11 | /kur hata davranışı (503'te kart kaybolması) |
| B-15 | D3 | P-11 | /kur geçmişi "İptal et" etkisiz |
| B-16 | D3 | P-11 | Geri alınamayan QA5 kalıntısı (MANUAL kur) |
| B-17 | D3 | P-13 | Kuruluş sihirbazı (**K-24'e bağlı dilim**) |
| B-18 | D2 | P-06 | Alan altı doğrulama sözleşmesi + iş kuralı sınırları |
| B-19 | D3 | P-12 | Storno görünürlüğü + 48-A parti geri alma |
| B-20 | D2 | P-09 | MFA API borçları (kurtarma kodu, uç adları, iz) |
| B-21 | D1 | P-02 | BEKLENEN_VARLIK sayacı gerçek sayıya |
| B-22 | D3 | P-11 | /bildirimler basit mod menüsünde yok |
| B-23 | D3 | P-11 | Zamanlayıcı döngüsünün canlı kanıtı |
| B-24 | D3 | P-11 | Zamanlanmış iş ayarları düzenlenemiyor |
| B-25 | D4 | P-15 | "Fişten Fatura Kes" ULID → kayıt seçici |
| B-26 | D5 | P-22 | /siparisler durum geçişi onaysız ve geri alınamaz |
| B-27 | D3 | P-13 | S-3f "kapandı" denen iki bulgunun yeniden ölçümü |
| B-28 | D6 | P-24 | DOM gerektiren kanıtlar (pasta getBBox) |
| B-29 | D4 | P-15 | Derin bağlantı / süzgeç sözleşmesi |
| B-30 | D4 | P-18 | /yetkiler ROL sütunu sticky değil |
| B-31 | D4 | P-16 | Pasif cariler satış listesinde seçilebiliyor |
| B-32 | D4 | P-18 | Karanlık temada satır vurgusu okunamıyor |
| B-33 | D5 | P-22 | /sayim DURUM sütununda düz metin "İptal Et" |
| B-34 | D2 | P-06 | Dolu formda ilk "Kaydet" etkisiz |
| B-35 | D2 | P-09 | Başarısız girişte kullanıcı varlığı ifşası |
| B-36 | D2 | P-09 | Tek SAHİP çıkmazı + devir akışı |
| B-37 | D2 | P-09 | Kullanıcı kaydı silinemiyor / pasife alınamıyor |
| B-38 | D3 | P-12 | /vezne Düzenle penceresinde İşçilik alanı yok |
| B-39 | D3 | P-13 | Aktif cari pasife alınamıyor |
| B-40 | D3 | P-13 | /urunler/agac kategori pasife alınamıyor |
| B-41 | D3 | P-12 | Kasa hareketi düzenlenemiyor |
| B-42 | D3 | P-12 | Banka dekontu ne düzenlenebiliyor ne stornolanabiliyor |
| B-43 | D3 | P-12 | Emanet hareketi düzenlenemiyor |
| B-44 | D5 | P-22 | Sayım oturumunun adı düzeltilemiyor |
| B-45 | D5 | P-22 | Gün sonu kasa sayımı hiçbir yerden girilemiyor |
| B-46 | D5 | P-22 | POS "Bağlı Kasa" düzeltme yolu yok |
| B-47 | D3 | P-13 | /isletme kur kaynağı ve maliyet yöntemi boşken kilitli |
| B-48 | D3 | P-13 | /yedekler iptal gerekçesi gösterilmiyor |
| B-49 | D3 | P-13 | UX-1-C3 gider pusulası satıcı kimliği + /uyum düğmeleri |
| B-50 | D0 | P-00 | QA turu 4 kalan 17 maddesi listelenmedi |
| B-51 | D0 | P-00 | S-3e açık kalanlarının S-3f'te kapandığı ölçülmedi |
| B-52 | D2 | P-08 | 34-A denetim zenginleştirme dilimi |
| B-53 | D1 | P-02 | mut.sh + 3 DB yedeği sürümlenmemiş |
| B-54 | D2 | P-06 | Gerekçeli düzenlenemezlik istisnaları (21 kalem) |
| **B-55** | D5 | P-22 | **(YENİ)** POS tahsilat varken "yeni kasaya taşı" göç akışı |
| **B-56** | D5 | P-22 | **(YENİ)** POS kasa değişiminin tahsilatlı reddi canlı denenmedi |
| **B-57** | D2 | P-07 | **(YENİ)** kur/guncel 130+ tekrar istek fırtınası + 401 |
| **B-58** | D0 | P-00 | **(YENİ)** Tur 4/5'in 119 açık + ≈59 yeni bulgusunun kimliklendirilmesi |

### Yeni işler (Y-1 … Y-42) — 42 kalem

| Kimlik | Dalga | Paket | Konu |
|---|---|---|---|
| Y-1 | D1 | P-04 | Şube izolasyonu global sorgu filtresine |
| Y-2 | D1 | P-05 | Eksik 5 unique kısıt |
| Y-3 | D3 | P-10 | Sunucunun kabul ettiği 7 alanı forma bağla |
| Y-4 | D4 | P-16 | Satış kalem satırına ayar/milyem/gram/has + iskonto |
| Y-5 | D4 | P-15 | Sunucu taraflı aranabilir seçici bileşeni |
| Y-6 | D4 | P-17 | Ortak yazdırma / PDF şablonu |
| Y-7 | D4 | P-17 | Pano fiyat ucu + vitrin/kur panosu |
| Y-8 | D3 | P-10 | Ürün görseli / galerisi |
| Y-9 | D4 | P-14 | Kenar çubuğu gruplaması + etiket çarpışmaları |
| Y-10 | D5 | P-20 | HURDA SATIŞI hareket türü + has/TL karşı bacağı |
| Y-11 | D5 | P-19 | Virman (kasa↔banka) + şubeler arası transfer |
| Y-12 | D5 | P-19 | Gider/masraf kategorisi + gider ekranı |
| Y-13 | D2 | P-08 | Fişe satıcı personel alanı |
| Y-14 | D2 | P-08 | hareketler tablosuna adet kolonu |
| Y-15 | D2 | P-06 | Düzeltme politikası rozeti + iç kod temizliği |
| Y-16 | D6 | P-25 | Stok yaşlandırma / ölü stok raporu (16. rapor) |
| Y-17 | D5 | P-21 | Avans / kapora işlemi |
| Y-18 | D5 | P-21 | Sipariş → satış dönüşümü |
| Y-19 | D5 | P-21 | Değişim (ürünü ürünle) |
| Y-20 | D5 | P-21 | Konsinye çıkış / iade |
| Y-21 | D5 | P-21 | EVRAK_CIRO ucu + rücu durumu |
| Y-22 | **1.1** | — | Emanet parça kimliği (**K-12 gelmeden açılmaz**) |
| Y-23 | D5 | P-21 | Rezerv / bloke adet kolonu |
| Y-24 | **1.1** | — | Müşteri taksit planı (P-23'ün Faz B'si, yer bırakılır) |
| Y-25 | D2 | P-08 | fatura_no serbest metnin emekliye ayrılması (**karar değişti**) |
| Y-26 | D4 | P-16 | Ürün kartının 9 eksik alanı + çoklu taş satırı |
| Y-27 | D4 | P-16 | Cari kartı eksikleri (TCKN/VKN, ünvan, segment…) |
| Y-28 | D8 | P-34 | Referans bütünlüğü: gerçek FK'lar |
| Y-29 | D4 | P-15 | Kabuk katmanı: arama, komut paleti, şube seçici |
| Y-30 | D6 | P-26 | Tamir/servis iş emrini deftere tam bağla |
| Y-31 | D6 | P-25 | Rapor Merkezi sayısı tutarlılığı (15 → 16) |
| Y-32 | D2 | P-08 | HareketTurleri sözleşmesi kodda zorlansın |
| Y-33 | D1 | P-02 | RotaBasliklari 42 rota ↔ 42 @page çırçırı |
| Y-34 | D6 | P-26 | Satınalma siparişi (PO) + mal kabul |
| Y-35 | D6 | P-26 | Sevk / satış-alış irsaliyesi |
| Y-36 | D6 | P-26 | Ustaya metal çıkışı (**K-13'e bağlı dilim**) |
| Y-37 | D6 | P-26 | Muhasebeleştirme: hesap kodu + muhasebe fişi |
| Y-38 | **1.1** | — | Set birleştirme / bozma |
| Y-39 | D2 | P-09 | Yetki matrisi otomatik üretimi + şube görünürlüğü |
| **Y-40** | D6 | P-26 | **(YENİ)** KDV 23/e özel matrah varsayılanı (**K-10'a bağlı**) |
| **Y-41** | D7 | P-32 | **(YENİ)** Ücretsiz pilot paketi |
| **Y-42** | D0 + D8 | P-00, P-35 | **(YENİ)** Triyaj (intake) kuralı |

### Kararlar (R-1 … R-19) — 19 kalem

| Kimlik | Dalga | Paket | Kapanış biçimi |
|---|---|---|---|
| R-1 | D7 | P-27 | Karar verildi: koşulsuz 1.0 |
| R-2 | D7 | P-28 | Karar verildi: koşulsuz 1.0 |
| R-3 | D7 | P-29 | Karar verildi: 1.0, belge hattında paralel |
| R-4 | D7 | P-30 | Karar verildi: 1.0 |
| R-5 | D7 | P-31 | Karar verildi: 1.0 |
| R-6 | D6 | P-25 | Karar verildi: 1.0, B-8'den sonra |
| R-7 | D7 | P-32 | Karar verildi: 1.0, ücretli tarihin ön şartı |
| R-8 | D7 | P-29 | Karar verildi: tek rakamlı oran ilan edilmez |
| R-9 | D0 + D8 | P-01, P-35 | Açılış ve kapanış mührü |
| R-10 | D1 | P-03 | Karar verildi: 1.0, tavan yalnız azalır |
| R-11 | D0 | P-01 | Ekip oyuyla mühürlenecek |
| R-12 | D0 | P-01 | Ekip oyuyla mühürlenecek |
| R-13 | D0 | P-01 | Oy 2-2; kurul kırar, kırılmazsa Tamer Bey mühürler |
| R-14 | D0 | P-01 | Ekip oyuyla mühürlenecek |
| R-15 | D0 | P-01 | Karar verildi: sıra 0 kabul, DALGA 4 buna göre dizildi |
| R-16 | D0 | P-00 | Belge üzerinden satır satır doğrulanacak |
| R-17 | D6 | P-23 | Karar verildi: EVET, vezne kuralı korunarak |
| R-18 | D6 | P-23 | Karar verildi: tümü ya da hiç; kısmi = mimari veto |
| R-19 | D0 | P-01 | Ekip oyuyla mühürlenecek |

### Dondurulmuş (D-1 … D-13) — 13 kalem
Tamamı **§ 6**'da. Tek istisna: **D-8 → 1.0'a alındı**, P-29 içinde.

### Kurucu kalemleri (K-1 … K-26) — 26 kalem
Tamamı **§ 4** tablosunda, her birinin "ihtiyaç tarihi" ve "gelmezse ne durur" satırıyla.

### Sayım kontrolü

| Küme | Adet | Nerede |
|---|---|---|
| B | 58 | 56'sı pakette, 2'si P-00'da açılacak |
| Y | 42 | 39'u pakette, 3'ü 1.1'de (Y-22, Y-24, Y-38) |
| R | 19 | Hepsi bir pakette kapanıyor |
| D | 13 | 12'si 1.1, 1'i (D-8) 1.0'da |
| K | 26 | Hepsi § 4'te |
| **TOPLAM** | **158** | **Yazılmamış kimlik: 0** |

---

## KAPANIŞ

Bu plan, kurucunun *"tek seferde, sonradan aa bunu unutmuşum demeyeceğin"* talebine şu üç mekanizmayla karşılık verir:

1. **Kapsam kaynağa bağlanır** (P-00) — unutulan şey görünür hâle gelir.
2. **Yeni bulgu kapıya bağlanır** (Y-42 triyaj) — damgasız bulgu kapıyı kırar.
3. **Sayaçlar geri dönüşsüzdür** — kapanan bir borç bir daha açılamaz.

Planın kendisi de bu kurallara tabidir: **147 → 158 kimlik**, **303 → 362,1 ajan-saat**, **37 → 36 paket**, **51 → 63 şube noktası**, **47 → 42 rota** düzeltmeleri bu belgede gizlenmedi, sayıldı ve nedeni yazıldı.

**Hiçbir merceğin HAYIR oyu yoktur. Plan MÜHÜRLENDİ; 50 bağlı koşul planın ayrılmaz parçasıdır.**

— **Tamer Bey**, 2026-09-18

---

## § 13 — EK: QA turu 6 (plan mühürlendikten 20 dakika sonra tamamlandı)

> **Dürüstlük notu (Anayasa md.1):** hasım turunun "unutulan" merceği koştuğu anda `ham/qa-turu-6.json` henüz yazılmamıştı (koşum sürüyordu). Bu ek, turun sonucunu plana bağlar; P-00'ın triyaj kuralı (Y-42) burada ilk kez uygulanır.

**Tur 6 sonucu (S-3e/S-3f sonrası ilk ölçüm, 11:29 yayını, 39 rota, ölçülemeyen rota 0):**

| Ölçü | Tur 5 | Tur 6 |
|---|---|---|
| Kritik | 1 | 1 (aynı: taslak fatura düzenlenemiyor → D2/D4 düzenlenebilirlik hattı, B- kimliği envanterde) |
| Yüksek | 26 | 12 |
| Düzenlenemeyen kalem | 49 | 37 |
| Görsel | 3,31 | 3,44 |
| Kullanıcı dostu | 2,92 | 3,03 |
| Önceki bulgular | — | 24 kapandı · 12 kısmen · 157 açık |

**Kapanan kurul engelleri:** 4 rotanın ölçülememesi (engel 15) ve MFA'nın canlı ölçülmemesi (engel 9) — `/ayarlar` MFA akışı bu turda uçtan uca test edildi.

**Tur 6'nın envanterde OLMAYAN iki yeni bulgusu — P-00 triyajıyla kimliklenir:**

| Yeni kimlik | Bulgu | Ciddiyet | Nereye girer |
|---|---|---|---|
| Y-43 | Kur ekranı 10 saniyede bir sunucuyu yokluyor; SignalR WebSocket el sıkışması düşüyor, itme kanalı kurulamıyor → saatte ~360 istek/sekme, uygulama kendi 429 sınırına takılıyor | ORTA / performans | DALGA 2 dayanıklılık paketi (kesin paket numarası P-00 triyajında verilir); kabul: itme kanalı kuruluyken HTTP anketi 0, yedek anket ≥ 60 sn ve sekme arka plandayken durur |
| Y-44 | Erişim jetonu WebSocket adresinde sorgu dizesi olarak taşınıyor (konsol ve erişim günlüğüne düşer) | DÜŞÜK / güvenlik | Mehmet'in güvenlik koşulları bloğu (DALGA 1-2): kısa ömürlü hub bileti ya da Sec-WebSocket-Protocol; erişim günlüğünde access_token maskesi |

Bu ekle kapsanan kimlik sayısı **158 → 160** olur. P-00 raporu bu iki kimliği KAPSAM-ENVANTERI.md'ye işlemeden DALGA 1 başlamaz.

---

## § 14 — EK: KURUCU TALİMATI 16 VE PLANA EKLENEN P-20b (21 Eylül 2026)

Plan mühürlendikten sonra kurucu ürün kararlarını ekibe devretti:

> *"program hakkında benim yapabileceğim tek şey ekranın rengi yazının tipi vs görsel olur;
> geri kalan ne varsa Türkiye'de ne kadar program varsa araştırmıştın oradan karar vermen
> lazım, orada yoksa nerede takıldıysan onu ayrıyeten araman."*

Bu, planın **§ 4 kurucu kalemleri** bölümünü daraltır: ürünün *ne yapacağına* dair kalemler
(K-13, K-23, K-10, K-24, K-25, K-12, K-14, K-15, K-21, K-22, K-26) artık **kurucuyu beklemez**;
ekip **kaynak göstererek** karar verir. Kurucuda yalnız görsel tercih ile imza/para/sözleşme/
kendi makinesi kalır (K-20, K-4, K-8, K-19, K-16, K-1, K-2, K-3, K-5, K-6, K-9, K-11).

**Kararlar ve kaynakları:** `dokumanlar/09-uygulama-kayitlari/KARARLAR-URUN-2026-09-21.md`.

### Plana eklenen paket — dürüst sayım (BK-33 kapsam freni)

**K-13 kararı planın bir dalını kapatırken bir dal AÇTI.** Karar ikiye ayrıldı: *eritme firesi*
1.0'a girmez (imalat kapsam dışı), ama *tahlil / mutabakat farkı* girer ve zorunludur — çünkü
hurda satışında alıcının kabul ettiği has ile bizim kaydettiğimiz has farklı çıkar ve bu fark
yazılmazsa **has-cari denk gelmez**; has-cari de bu ürünün çekirdek vaadidir.

| Paket | Ad | Kimlik | Sürüm | Tahmin |
|---|---|---|---|---|
| **P-20b** | Hurda satışında tahlil / mutabakat farkı | Y-10 (kalan dilim) + K-13 | 1.0 | 120 ajan-dk |

* **Önkoşul:** P-19 (`CiftBacakFabrikasi`) · P-20 (hurda satışı)
* **DoD:** iki has alanı ayrı (bizim kaydımız / alıcının kabul ettiği) · fark ≠ 0 ise gerekçesi
  ZORUNLU ayrı bacak · tek `SaveChangesAsync` içinde atomik · fark "fire" diye adlandırılmaz
* **Sayaçlar (cırcır):** `TAHLIL_FARKI_ESSIZ_BACAK = 0` · `HURDA_FIRE_SIZINTISI = 0`
  (ikincisi K-13'ün "eritme firesi girmez" yarısının makine kontrolüdür)

**Paket sayısı 36 → 37.** Bu bir gerileme değil, kararın doğurduğu işin dürüst sayılmasıdır
(planın kendi BK-33 kuralı: dalga başına doğan yeni kimlik/paket gizlenmez, sayılır).

### § 14.2 — EK PAKET: P-36 FRONTEND ŞERİDİ (21 Eylül 2026)

**Kurucu sordu:** *"bu işler arasında front end ajanı var mı?"* — **Ölçülmüş cevap: HAYIR.**
Bugüne kadar her paketi tek ajan *"Taşkın backend + Ertuğ frontend rolü"* diye ikisini birden
üstlenerek yazdı (`betikler/dalga-uygula.js:73`). Sonuç ölçümde görünüyor:

| QA turu | 5 | 6 | **7** |
|---|---|---|---|
| Ortalama **görsel** | 3,31 | 3,44 | **3,64** |
| Ortalama **kullanıcı dostu** | 2,92 | 3,03 | **2,98** |

*(Ham veriden yeniden hesaplandı: `ham/qa-turu-5/6/7.json`. Özet ajanlarının yazdığı rakamlar
değil, rota puanlarının ortalamasıdır.)*

Görsel yavaş yükselirken **kullanıcı dostu üç turdur yerinde sayıyor, hatta düşüyor.** Kurucunun
en eski sabit talimatlarından biri "en güzel arayüz"dü ve bu kalemin **sahibi yoktu** — her paket
kendi ekranını yaparken bütünü kimse gözetmiyordu.

| Paket | Ad | Sürüm | Tahmin | Sıra |
|---|---|---|---|---|
| **P-36** | Görsel dil + kullanıcı dostu eşiği (frontend şeridi) | 1.0 | 300 ajan-dk | **dalga 6'nın İLK paketi** |

* **Kapsam:** yalnız arayüz — ortak kabuk ve bileşenler (tek yerden 46 ekranı düzeltir), boş durum
  metinleri, hata mesajlarının dili, odak/klavye akışı, tipografi ve boşluk ölçeği.
  **Yeni özellik yok, sunucu sözleşmesine dokunulmaz.**
* **Kurucunun kendi şeridi:** renk ve yazı tipini biz tahmin etmiyoruz — `/ayarlar`a **Görünüm**
  bölümü açılır (tema, 3-4 hazır renk şeması, yazı tipi ölçeği); hazır şemalar **WCAG AA 4.5:1**
  kontrastı geçmek zorundadır. Kurucunun ürüne gerçekten dokunabildiği tek yer burası olur.
* **Kabul eşiği (cırcır):** paketten sonraki QA turunda **görsel ≥ 4,0** ve
  **kullanıcı dostu ≥ 3,6**. Puanı bulgu gizleyerek, işlev silerek ya da fikstürü değiştirerek
  yükseltmek paketi KALDI yapar.
* **Sayaçlar:** `KONTRAST_IHLALI = 0` · `SABIT_RENK` tavanı (14) **düşürülür** ·
  `BOS_DURUM_METINSIZ = 0` · `ODAK_TUZAGI = 0`.

**Paket sayısı 37 → 38.** Yine gizlenmiyor, sayılıyor (BK-33).

### § 14.3 — EK PAKET: P-37 MENÜ + KARŞILAMA EKRANI (kurucu talimatı 20, 21 Eylül)

> *"sadece dropdownlar için değil tüm inputlar için bize ait birer tane component olmalı, her yerde
> aynısı olur, düzenlemesi özellik eklemesi de kolay olur. Menü arama menünün üzerinde olmalı,
> ayrıca menüde favoriler olmalı kullanıcıya özel, ayrıca grup içerisindeki başlıklar sürükle bırak
> mantığıyla yer değiştirebilir olmalı. Ayrıca ilk sayfa menü olarak pencere pencere olan bir ekran
> karşılamalı, en uygun yerlere hoş geldin kullanıcı ismi olmalı ve havanın kaç derece olduğu gibi,
> kullanıcıyı ekrana kilitleyecek eline telefon aldırmayan bir tasarım olmalı."*

**P-36'ya eklendi:** tüm girdiler için ortak bileşen ailesi (`Bilesenler/Form/`: Metin, Sayi, Para,
Gram, Tarih, Telefon, Eposta, CokSatir, Onay, Secim, Anahtar + Secici). Sayaçlar `HAM_INPUT → 0` ve
`SATIRICI_RENK/PX` yalnız azalır.

**Yeni paket P-37** (1.0 · 420 ajan-dk · P-36'dan sonra):

| Konu | Karar |
|---|---|
| **Menüde arama** | Kutu **menünün en üstünde**, her zaman görünür. Ctrl/Cmd+K çalışır, odak kutuya gider, hem kayıtları hem **ekran adlarını** bulur. |
| **Favoriler** | Ekran yanında yıldız; favoriler menünün en üstünde, **kullanıcı bazında** saklanır. |
| **Sıralama** | Grup içi sürükle-bırak; **klavyeyle de** yapılabilir (Alt+↑/↓), dokunmatikte çalışır, "sırayı sıfırla" var. Gruplar arası taşıma bu sürümde yok. |
| **Karşılama ekranı** | `/` kutucuklardan oluşur: günün satış/alış/nakit özeti · anlık altın ve döviz · bekleyen işler · son işlemler · hızlı eylemler. Kutucuklar dizilebilir/gizlenebilir. |
| **Hoş geldin + hava** | Saate göre selam + kullanıcı adı + tarih + **hava (derece)**. Hava dış kaynaktır: internet yoksa **sessizce kaybolur**, sayfayı BEKLETMEZ, konum izni İSTEMEZ, kaynağın lisansı rapora yazılır. |

**"Ekrana kilitleyen tasarım" ölçülebilir kılındı** — his değil, dört kabul ölçütü:
(1) sabahki beş sorunun beşi de kaydırmadan görünür, (2) kutucuklar birbirini beklemez (iskelet
gösterilir), (3) kur ve özet kendiliğinden tazelenir, (4) en sık üç işlem tek tıkla başlar.
*"Şık duruyor" bir kabul ölçütü DEĞİLDİR.*

**Önce kapatılacak hata (frontend ajanı canlı kanıtla buldu):** `Servisler/YetkiServisi.cs:30`
`catch { moduller = []; }` — yetki isteği başarısız olunca menü **36 bağlantıdan 1'e düşüyor** ve
arama hiçbir ekran bulamıyor, tek uyarı bile çıkmadan. Kurucunun "menüde arama yapamıyorum"
şikâyetinin kökü bu olabilir. Sessiz yutma kalkacak, regresyon testi zorunlu.

**Paket sayısı 38 → 39.**
