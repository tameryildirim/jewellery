# PAKET B-2 — ALTIN POZİSYONU: PANELİN ÜÇÜNCÜ KARTI (SUNUCUDA HESAPLANIR) — Uygulama raporu

Rol: Taşkın (backend) + Ertuğ (frontend) · Dalga S-B · 2026-09-23 · Durum: **BİTTİ (paket kapısı, 7/7)**.
Paneldeki yerleşim DEMO şeridine bırakıldı (aşağıda "Açık kalanlar" 1. madde).

Bu rapor iki oturumun toplamıdır. İlk uygulayıcı kodu yazdı, beyanı YARIM'dı (canlı doğrulama yok, rapor yok).
Hasım doğrulayıcı 5 bulgu çıkardı. Bu tur o bulguları kapattı (§ 0), sonra kapıyı baştan ölçtü.

---

## 0. Önceki doğrulamanın bulguları: ne yapıldı

| Bulgu | Önem | Bu turda yapılan | Kanıt |
|---|---|---|---|
| Canlı doğrulama yok, rapor yok | KRİTİK | 5193'te API başlatıldı. `canli.py` koşuldu (19 adım, hepsi beklenen kodla döndü). Test kiracıları silindi. Bu rapor yazıldı. | § 6, § 7 |
| Kart hiçbir ekranda yok | KRİTİK | **Kapatılmadı, bilerek.** `Panel.razor` DEMO şeridinin dosyası ve devir notu ona dokunmayı yasaklıyor. Orkestratör DEMO şeridiyle planlamalı: tek satır `<AltinPozisyonuKarti />` eklenir ve TEK DOKUNUŞ gerekçesi kapı kaydına yazılır. | § 9-1 |
| RolGerekli mutasyonu (M-B2-9) hayatta kalıyor | ORTA | Yeni HTTP testi **B2H_05**: SAHİP, TEZGÂHTAR'a yetki matrisinden `rapor` modülünü verir. `/yetkiler/benim` modülün açık olduğunu kanıtlar. Pozisyon yine 403 döner. Aynı matriste MUHASEBE 200 alır. M-B2-9 yeniden koşuldu ve **2 kırmızı** verdi. | § 4 |
| 3 mutasyon derleme yarışı yüzünden geçersizdi | DÜŞÜK | Yarışa dayanıklı `kos.sh` yazıldı. Derleme hatası ya da DLL yükleme hatası sonuç sayılmıyor, 20 sn sonra yeniden deniyor (en çok 6 kez). M-B2-4/5/7 yeniden koşuldu. M-B2-5'in ilk hâlinde betik hatası vardı: perl `$ENV{REP}` içindeki `$1`'i genişletmedi, kaynağa düz "$1" yazıldı ve CS1056 hatası çıktı. Yani bu bir yarış değildi. İleri bakış (`(?=…)`) ile **M-B2-5b** olarak yeniden koşuldu ve geçerli kırmızı alındı. | § 4 |
| HurdaStokKapisi.cs git'te izlenmiyor | DÜŞÜK | Paket öncesi hâl `scratchpad/mut-yedek/P-20b/HurdaStokKapisi.cs` yedeğinde duruyor (21.09 09:23; sha256 `87b3e2da…fa76e86`). `diff` çıktısı yalnız **ekleme** gösteriyor: `71a72,82` (11 satır; `KiraciKirilimi` + belge yorumu). Silinen ya da değişen satır yok. Şimdiki sha256 `072fe44c…f0123`. | § 1 |

---

## 1. Yapılanlar

**Sunucu (Taşkın):**
- `Kuyumcu.Api/Modules/Pozisyon/PozisyonModulu.cs` (yeni). İki bölümden oluşur:
  - `PozisyonServisi.Hesapla`: maden başına NET = VİTRİN + HURDA + CARİ ALACAK − CARİ BORÇ.
    - Vitrin: stoklar ⋈ ürünler; madeni ayar belirler. Konsinye mal HARİÇ tutulur ve bilgi olarak ayrı döner.
    - Hurda: `HurdaStokKapisi.KiraciKirilimi`.
    - Cari: VERESIYE/TAHSILAT/CARI_DUZELTME/ACILIS türleri. İşaret sözleşmesi K4: bakiye += −HasGrami.
    - Emanet pozisyona GİRMEZ (nötr) ve bilgi olarak döner.
    - `cari_bakiyeler` (ALTIN) ile defter karşılaştırılır; aradaki fark `defterBakiyeFarki` alanında döner.
  - `PozisyonServisi.Cariler`: kırılım listesi (ALACAK/BORÇ). En çok 500 satır döner; sayfalıdır, `dahaFazlasiVar` alanı taşır. Her okuma `DenetimServisi.ListeOkumaKaydet` ile iz bırakır (KVKK).
  - Uçlar: `GET /api/v1/pozisyon` ve `GET /api/v1/pozisyon/cariler`. Kapılar: `[Authorize]`, `[RolGerekli("SAHIP","MUDUR","MUHASEBE")]` ve `[ModulYetkisi("rapor")]`.
- **Maden ayrımı P-45'in yardımcısıyla yapılır; yeniden yazılmadı.** Kullanılan yardımcılar: `HareketToplami.Coz` / `HareketToplami.MadenKodu` (boş ya da NULL maden = ALTIN). Altın ile gümüş TOPLANMAZ. Ana sayı yalnız ALTIN'dır, diğer madenler `digerMadenler` altında kendi satırlarında döner. Yanıtta `toplamHas` gibi birleşik bir alan YOK.
- `Kuyumcu.Api/Modules/Hurda/HurdaStokKapisi.cs`: yalnız ekleme (satır 72–82). `KiraciKirilimi(db, tenantId)` eklendi. Hurda tür listesi tek kaynakta kalır.
- `Kuyumcu.Api/Modules/ModulKayit.cs:56`: `s.AddScoped<Pozisyon.PozisyonServisi>();` (git diff'te silinen satır: 0).
- `Kuyumcu.Contracts/Sayfalar/PozisyonSozlesmeleri.cs` (yeni): `AltinPozisyonuYaniti`, `MadenPozisyonu`, `PozisyonCariSayfasi`, `PozisyonCariSatiri`.
- Kiracı: her sorgu `TenantId == t` koşulunu AÇIKÇA taşır. Global süzgeç de ayrıca çalışır (savunma derinliği). Bunu B2_11 ve M-B2-5b/M-B2-14 kanıtlar.

**Ekran (Ertuğ):**
- `Kuyumcu.Web/Bilesenler/AltinPozisyonuKarti.razor` (yeni).
  - Taslaktaki kartı kurar: başlık "Altın pozisyonum", büyük sayı (işaretli, "g has"), altında Vitrin / Alacak / Borç / Hurda kırılımı. Emanet ve konsinye "dışarıda" bilgisi olarak görünür. Diğer madenler ayrı satırda ve "toplanmaz" notuyla durur. Kartın alt metni: "(bugün itibarıyla — dönemden bağımsız)" (UX-S33).
  - Alacak ve Borç sayıları düğmedir; tıklanınca `/pozisyon/cariler` isteği atılır (UX-S23). Liste sınırı yazılır ve "daha fazlası var" uyarısı çıkar (UX-S29). Hurda sayısı `/hurda` sayfasına bağlanır.
  - 403 alındığında yetki notu gösterilir (UX-S28). Sunucu hatasında hata bandı çıkar, boş durum çıkmaz.
  - `DegismezIpucu`: sayı türetilmiştir; düzeltme yolu kaynak kayıtlardır (sayım, cari düzeltme fişi, hurda ekranı).
- `Kuyumcu.Core/I18n/PozisyonSozlugu.cs` (yeni, TR+EN). Ortak `Sozlukler.cs` hub'ına DOKUNULMADI.
- Yeni DOM id'leri `pozisyon-*` önekini taşır (20 id). Mevcut id kaldırılmadı.

**Migration:** gerekmedi. Yeni tablo ya da kolon yok; pozisyon TÜRETİLİR.

## 2. Sapmalar ve gerekçeleri

1. **Hurda NET'e dahil edildi.** Taslakta yalnız "Vitrin + Alacak" görünüyor. Hurda da işletmenin elindeki has altındır ve P-20'den beri defterde yaşar, `stoklar`'da değil. Hurda dışarıda bırakılırsa pozisyon hurda kadar eksik görünür. Kartta ayrı kırılım satırı olarak gösterildi.
2. **Emanet ve konsinye pozisyon dışında, ama görünür.** Emanet için elde +g ve sahibine −g vardır, net 0 eder. Konsinye mal bize ait değildir. İkisi de "dışarıda" bilgisi olarak listelenir.
3. **Kart panele yerleşmedi.** `Panel.razor` DEMO şeridinin dosyası (devir notunda yasak). Bileşen hazır ve bUnit'te sınandı. Yerleştirme orkestratöre bırakıldı.
4. Mevzuat hükmü yok: pozisyon iç yönetim göstergesidir. Vergi, KDV, MASAK ya da tevsik hükmü içermez, bu yüzden birincil kaynak araştırması gerekmedi.

## 3. Test sayıları

| Ölçüm | Değer |
|---|---|
| Paket öncesi tam süit (19:01, `pB2/test-once.log`) | Toplam **4088**, Başarısız 1 (devir notundaki OlcumKapisi kırmızısı) |
| Bu paketin testleri | **30** (28 önceki tur + 2 yeni: B2H_05 × 2 yol). Filtreli koşum 30/30 yeşil (`pB2/t3.log`). |
| Paket sonrası tam süit | Toplam **4200**. Artış = bu paketin 30 testi + diğer şeritlerin eklemeleri. **Silinen test yok.** |

Test listesi:
- DB (AltinPozisyonuTestleri): B2_01…B2_11.
- HTTP (AltinPozisyonuHttpTestleri): B2H_01 (401), B2H_02 (TEZGÂHTAR 403, modül kapısı), B2H_03 (SAHİP 200, camelCase, `toplamHas` yok), B2H_04 (400/200), **B2H_05 (yeni: `rapor` modülü verilmiş TEZGÂHTAR rol kapısından 403 alır, MUHASEBE 200 alır)**.
- bUnit (AltinPozisyonuKartiTestleri): B2K_01…B2K_08.

**Tam süit nasıl ölçüldü (paralel şerit yarışı):** Aynı ağaçta başka şeritler eşzamanlı `dotnet build` koşturuyor.
1. İlk yerinde koşum (`pB2/tam.log`) geçersiz sayıldı. 759 hatanın 750'si DLL yükleme hatasıydı (`FileNotFoundException Microsoft.IdentityModel.Tokens / BCrypt / deps.json`): bin dizini koşum sırasında başka şeritçe silindi.
2. Test çıktısı scratchpad'e kopyalandı ve koşum orada yapıldı (`pB2/izole/bin`, trx: `scratchpad/trx-pB-2`). Sonuç: 4200 test, 3898 geçti, 302 başarısız (247 ayrı yöntem). Başarısızların tamamı kaynak tarayan testlerdi. Bu testler depo kökünü DLL konumundan yukarı yürüyerek bulur, yalıtılmış kopyada da kökü bulamadılar. Yani ürün hatası değiller.
3. Bu 247 yöntemin ait olduğu 44 sınıf yerinde yeniden koşuldu (trx: `pB2/trx-yer-1-1790183305/yer.trx`). Sonuç: 623 testten 622'si geçti. Başarısız tek test: `OluIcBaglantiTestleri.Hicbir_ic_baglanti_OLU_degildir`, gerekçesi "`/ekranlar ← KenarCubugu.razor`". Bu S-1 şeridinin yarım işi: `Layout/*` ve `Pages/Ekranlar.razor` S-1'in dosyaları. Aynı filtreyle birkaç dakika önceki yerinde koşum 623/623 yeşildi (`pB2/t-yer.log`); aradaki fark S-1'in canlı düzenlemesi.
4. **Paket kapısı (7) — yeni kırmızı üretmeme:** `AltinPozisyonu*` adlı hiçbir test kırmızı değil. Tek kırmızı B-2'nin dokunmadığı dosyalarda. Devir notundaki `denetim_sh_HER_CAGRI_DIZININDEN_AYNI_KARARI_VERIR` kırmızısı bu ölçümde YEŞİLDİ (P-30b kapatmış görünüyor). **FARK = 0.**

## 4. Mutasyon tablosu

Yöntem: yedek `scratchpad/mut-yedek/B-2/` altına alınır. Perl ile TEK eşleşmeli değişiklik yapılır. `FullyQualifiedName~AltinPozisyonu` filtresiyle test koşulur. Yedek geri kopyalanır ve `cmp` ile doğrulanır. Mutasyon sonuçlarından sonra kaynak `cmp` ile orijinale BİREBİR AYNI; son temiz derleme 0 Uyarı / 0 Hata verdi.

| Kod | Değişiklik | Kırmızıya dönen test(ler) | cmp |
|---|---|---|---|
| M-B2-1 | Emanet türleri cari has türlerine eklendi | B2_04_emanet_ve_konsinye_pozisyona_girmez_bilgi_olarak_doner | AYNI |
| M-B2-2 | Vitrinde maden süzgeci kaldırıldı (altın + gümüş toplanır) | B2_03_gumus_has_grami_altina_eklenmez_ayri_satirda_doner | AYNI |
| M-B2-3 | Konsinye hariç tutma kaldırıldı | B2_04_emanet_ve_konsinye_pozisyona_girmez_bilgi_olarak_doner | AYNI |
| M-B2-4 (yeniden, geçerli: deneme 5) | NET'ten hurda düşürüldü | B2_05_hurda_has_grami_pozisyona_eklenir | AYNI |
| M-B2-5b (yeniden, geçerli) | Cari sorgusundan `TenantId == t` kaldırıldı | B2_11_sistem_baglaminda_da_acik_tenant_kosulu_izolasyonu_saglar | AYNI |
| M-B2-6 | Borç işareti çevrildi | B2_02_cari_alacak_ve_borc_ayri_sayilir_ve_nete_isaretle_girer | AYNI |
| M-B2-7 (yeniden, geçerli) | Alacak sırası ters çevrildi | B2_08_cari_kirilimi_yon_sira_sinir_ve_denetim_izi | AYNI |
| M-B2-8 | Denetim izi (ListeOkumaKaydet) silindi | B2_08_cari_kirilimi_yon_sira_sinir_ve_denetim_izi | AYNI |
| **M-B2-9 (yeniden, önceden hayatta kalıyordu)** | `[RolGerekli("SAHIP","MUDUR","MUHASEBE")]` silindi | **B2H_05_rapor_modulu_verilmis_tezgahtar_rol_kapisindan_403_alir** (her iki yol) | AYNI |
| M-B2-10 | Kartta 403 dalı 401'e çevrildi | B2K_05_yetkisiz_rol_403_yetki_notu_gorur_hata_degil | AYNI |
| M-B2-11 | Büyük sayının "+" işareti kaldırıldı | B2K_01_buyuk_sayi_ve_kirilim_sunucudan_gelir, B2K_02_gumus_ayri_satirda_altina_eklenmez_ham_kod_basilmaz | AYNI |
| M-B2-12 | Defter-tablo karşılaştırmasından emanet düşürüldü | B2_04_emanet_ve_konsinye_pozisyona_girmez_bilgi_olarak_doner | AYNI |
| M-B2-13 | Limit üst sınırı kaldırıldı | B2_09_kirilim_gecersiz_girdiyi_400_ile_reddeder(limit: 501) | AYNI |
| M-B2-14 | Emanet sorgusundan `TenantId == t` kaldırıldı | B2_11_sistem_baglaminda_da_acik_tenant_kosulu_izolasyonu_saglar | AYNI |

Kayıtlar: `scratchpad/pB2/mut-sonuc.txt` (1–14, önceki tur) ve `scratchpad/pB2/mut2-sonuc.txt` (9, 4, 5, 7, 5b; bu tur). Önceki turdaki M-B2-4/5/7 satırları (build_hata>0) GEÇERSİZ sayıldı ve yerini bu turun satırları aldı.

## 5. Denetim ve "mevcut hali bozulmasın" altılısı

| # | Koşul | Sonuç |
|---|---|---|
| 1 | `dotnet build Kuyumcu.sln --nologo` | **0 Uyarı, 0 Hata** (`pB2/build6.log`; mutasyonlardan sonra `b8.log` da temiz) |
| 2 | Tam süit 0 başarısız ve test sayısı düşmez | Sayı 4088 → 4200, düşmedi. B-2 kırmızısı 0. Kalan 1 kırmızı S-1'e ait (§ 3). Ağaç ölçüsü orkestratörün kapısıdır. |
| 3 | `bash ./denetim.sh` | **DENETİM TEMİZ** (çıkış 0; `pB2/denetim3.log`). "Sınırsız ToListAsync" 145, tavan 145; paket öncesinde de 145'ti (`denetim1.log`). |
| 4 | Mevcut uç kaldırılmadı | `grep -rn "\[Http" Kuyumcu.Api --include="*.cs" \| wc -l` = **369**. Bunların 2'si bu paketin yeni uçları; paket öncesi 367. Kaldırılan uç yok. |
| 5 | Mevcut DOM id'si değişmedi | B-2'nin dokunduğu Web dosyası yalnız yeni `AltinPozisyonuKarti.razor`; git'te izlenen hiçbir razor değişmedi. Envanterde silinen id = 0 (madde 6). |
| 6 | Özellik kaybı 0 | `envanter-uret.sh --cikti pB2/envanter-sonra.json`, kimlik bazlı karşılaştırma (rota, menü, API, eylem, bileşen id'si). `ENVANTER-S0.json` tabanına göre SİLİNEN **0**, eklenen 44. Paket öncesi (`envanter-once.json`) tabanına göre SİLİNEN **0**, eklenen 24: 2 API ucu, 20 `pozisyon-*` id'si ve 2 `/kurulum` ögesi. `/kurulum` ögeleri YAYIN şeridinin işi. |
| — | `Hareketler.Add(` | Api ve Web içinde yalnız `Data/HareketFabrikasi.cs` (B-2 deftere YAZMAZ, salt okur). |

Dokunulan sayfalar (TEK DOKUNUŞ): **Pages/ altında hiçbiri.** Yalnız yeni bileşen `Bilesenler/AltinPozisyonuKarti.razor`.

## 6. Canlı doğrulama (port 5193, `kuyumcu_net`)

API şu komutla başlatıldı: `dotnet run --no-build --no-launch-profile --urls http://localhost:5193` (PID 19536). Kiracı yalnız davetle açılır; betik tek kullanımlık davet satırı ekledi. Jeton ve parola ekrana basılmadı.

İlk koşum (kiracı PAKETB-2-TEST) betik fikstürü yüzünden eksik kaldı: alış `VERESIYE` ödeme türü kabul etmiyor, düzeltme ve emanet `kurSnapshotId` istiyor. Sunucu bunları doğru biçimde 400 ile reddetti. Betik düzeltildi ve ikinci kiracıda (PAKETB-2-TEST-2) koşuldu. Aşağıdaki tablo bu ikinci koşumdur. İki kiracı da silindi.

| # | Adım | Beklenen | Gerçek | Özet |
|---|---|---|---|---|
| 1 | POST /auth/kayit | 201 | 201 | kiracı açıldı |
| 2 | GET /kur/guncel | 200+id | 200 | kur var |
| 3 | GET /pozisyon (boş kiracı) | 200, net 0 | 200 | net=0, diğer=0 |
| 4–5 | POST /cari ×3 | 201 | 201 | tedarikçi, müşteri, emanetçi |
| 6 | POST /urunler K22 10 g | 201 | 201 | hasGrami=9.16 maden=ALTIN |
| 7 | POST /urunler G925 10 g | 201 | 201 | hasGrami=9.25 maden=GUMUS |
| 8 | POST /islemler/alis K22 × 3 (NAKIT) | 200 | 200 | stok girdi |
| 9 | POST /islemler/alis G925 × 2 (NAKIT) | 200 | 200 | stok girdi |
| 10 | POST /cari/{müşteri}/veresiye has 5 | 200 | 200 | müşteri 5 g borçlu |
| 11 | POST /cari/{toptancı}/duzeltme has −3 | 200 | 200 | biz 3 g borçluyuz |
| 12 | POST /emanet/giris K24 2 g | 200 | 200 | emanet 2 g |
| **13** | **GET /pozisyon** | **net = 27,48 + 5 − 3 = 29,48** | **200** | **vitrin=27.48 alacak=5.0 borc=3.0 emanet=2.0 net=29.48 fark=0.0** |
| 14 | GET /pozisyon, GÜMÜŞ ayrı | 18,5 g ayrı, altına eklenmez | 200 | gumus=18.5 madenlerToplanmaz=True, birleşik alan yok |
| 15 | GET /pozisyon/cariler ALACAK | 200, 1 satır, 5 | 200 | toplam=1 [('B2 Müşteri', 5.0)] |
| 16 | GET /pozisyon/cariler BORC | 200, 1 satır, −3 | 200 | toplam=1 [('B2 Toptancı', −3.0)] |
| 17 | GET /pozisyon/cariler?maden=BAKIR | 400 | 400 | "Bilinmeyen maden: 'BAKIR'." |
| 18 | GET /pozisyon/cariler?limit=501 | 400 | 400 | "Limit 1–500 arasında olmalı." |
| 19 | GET /pozisyon (jetonsuz) | 401 | 401 | — |

Emanet (2 g) NET'e girmedi: 27,48 + 5 − 3 = 29,48 ve emanet yalnız bilgi alanında. Gümüş (18,5 g) altına eklenmedi.

**SQL kanıtı** (`pB2/canli-sql.txt`, kiracı 01M37JFRTV4BKAQQPA3KC64SHN):

```
stoklar ⋈ urunler:  K22  konsinye=0  27.480000   |  G925  konsinye=0  18.500000
hareketler (cari_id dolu), -SUM(has_grami):
  ALIS ALTIN -27.48 (1) · ALIS GUMUS -18.50 (1)   ← nakit alış; cari has türü DEĞİL, pozisyona cariden girmez
  VERESIYE ALTIN 5.00 (1) · CARI_DUZELTME ALTIN -3.00 (1) · EMANET_GIRIS ALTIN -2.00 (1)
cari_bakiyeler ALTIN toplam: 0.000000   (defter: 5 − 3 − 2 = 0 → defterBakiyeFarki 0.0 ✓)
denetim_kayitlari: OKUMA/Cari "pozisyon-cariler ALTIN/ALACAK", "pozisyon-cariler ALTIN/BORC"  (2 iz ✓)
```

Not: 7b adımında `GET /kasalar` yanıtından TL kasa seçilemedi (kasa=YOK), yine de alış 200 döndü. Sunucu varsayılan kasayı kendisi seçiyor (UX-S22). Bu B-2'nin konusu değil; bilgi olarak yazıldı.

**Kapanış:**
- API durduruldu: `kill 19536`, ardından `lsof -iTCP:5193 -sTCP:LISTEN` boş.
- İki test kiracısı `tenant_id` kolonu taşıyan 70 tablonun tümünden silindi. Kalan satır: 0.
- 2 davet satırı silindi.
- `tenants` sayısı: önce **10**, sonra **10**.
- `QA-TEST-%` kiracılarına (4 adet) dokunulmadı.

## 7. DoD sözleşmesi

| Varlık / kayıt | Düzenleme ucu | Neden salt okunur / düzeltme yolu |
|---|---|---|
| Altın pozisyonu (kart sayısı) | YOK (bilerek) | Kayıt değildir, sunucuda TÜRETİLİR; saklanan satırı yoktur. Kartta `DegismezIpucu` gerekçeyi ve düzeltme yolunu yazar: stok farkı için sayım, cari has farkı için düzeltme fişi (`POST /cari/{id}/duzeltme`), hurda için hurda ekranı. |
| Pozisyon cari kırılımı | YOK (bilerek) | Okuma görünümüdür. Her satır cariye gider; düzeltme carinin kendi ekranından yapılır. |
| Yeni tablo / kolon / finansal kayıt | — | Oluşturulmadı. B-2 deftere yazmaz, para el değiştirmez. Tevsik, MASAK, gün kilidi ve kasa kapılarına konu değildir. |

Yetki: `[Authorize]` + `[RolGerekli("SAHIP","MUDUR","MUHASEBE")]` (tam eşleşme) + `[ModulYetkisi("rapor")]`. Denetim: kırılım okuması `DenetimServisi.ListeOkumaKaydet` ile iz bırakır. Kartta her düğme gerçek istek atar (B2K_07).

## 8. Değişen dosyalar (`git status --porcelain | grep -v -E "/(bin|obj)/"` içinden B-2'ye ait olanlar)

```
 M dotnet/Kuyumcu.Api/Modules/ModulKayit.cs                    (+1 satır, 56)
?? dotnet/Kuyumcu.Api/Modules/Pozisyon/PozisyonModulu.cs        (yeni)
?? dotnet/Kuyumcu.Api/Modules/Hurda/HurdaStokKapisi.cs          (dizin izlenmiyor; yalnız ekleme 72–82, § 0)
?? dotnet/Kuyumcu.Contracts/Sayfalar/PozisyonSozlesmeleri.cs    (yeni)
?? dotnet/Kuyumcu.Core/I18n/PozisyonSozlugu.cs                  (yeni)
?? dotnet/Kuyumcu.Web/Bilesenler/AltinPozisyonuKarti.razor      (yeni)
?? dotnet/Kuyumcu.Tests/AltinPozisyonuTestleri.cs               (yeni)
?? dotnet/Kuyumcu.Tests/AltinPozisyonuHttpTestleri.cs           (yeni; bu tur B2H_05 eklendi)
?? dotnet/Kuyumcu.Tests/AltinPozisyonuKartiTestleri.cs          (yeni)
?? dokumanlar/09-uygulama-kayitlari/PAKET-B-2-uygulama-raporu.md
```

## 9. Koşul karşılama

| Koşul | Durum |
|---|---|
| Sunucuda hesaplanır, maden bazlı, altın ile gümüş toplanmaz (P-45 yardımcısı) | ✓ B2_03, M-B2-2, canlı 14 |
| Kiracı izolasyonu | ✓ B2_06, B2_11, M-B2-5b, M-B2-14 |
| Yetki kapısı | ✓ B2H_01/02/05, M-B2-9 |
| Bileşenler tek tek doğrulanabilir (UX-S23) | ✓ `/pozisyon/cariler` ile ALACAK/BORÇ kırılımı ve hurda bağlantısı; B2K_03/04, canlı 15–16 |
| UX-S28/S29/S30/S31/S33 | ✓ B2K_05/06/08, sınır ve dahaFazlasiVar, "dönemden bağımsız" alt metni |
| Panelin üçüncü kartı olarak GÖRÜNÜR | ✗ Paket dışı: Panel.razor DEMO şeridinin dosyası (Açık kalanlar 1) |
| Paket kapısı (1)–(7) | ✓ 7/7 |

## 10. Açık kalanlar

1. **Kartın panele yerleşmesi (orkestratör + DEMO şeridi).** `Pages/Panel.razor` içine tek satır `<AltinPozisyonuKarti />` eklenmeli. Taslağa göre yeri üçüncü kart. TEK DOKUNUŞ gerekçesi kapı kaydına yazılmalı. Yerleşince paneldeki kart hem bUnit hem canlı ortamda doğrulanmalı. Bileşen hazır: 8 bUnit testi yeşil.
2. **Tam süitteki başkasına ait kırmızı:** `OluIcBaglantiTestleri.Hicbir_ic_baglanti_OLU_degildir`, gerekçesi "/ekranlar ← KenarCubugu.razor". Sahibi **S-1** (Layout/*, Pages/Ekranlar.razor). Aynı filtreyle önceki koşumda yeşildi; S-1'in canlı düzenlemesi.
3. **Paralel şerit derleme yarışı.** Başka şeritlerin eşzamanlı derlemesi bin dizinini siliyor. Paket içi ilk tam koşumu (759 yükleme hatası) bu yüzden geçersiz sayıldı. Orkestratörün tam süit ölçümü şeritler durunca yapılmalı.
4. **Olası yan etki, açıkça bildiriyorum.** Geçersiz ilk tam koşumdan sonra `pgrep -f "dotnet test"` bir PID (96598) döndürdü ve ben ona `kill` gönderdim. Kendi koşumum o sırada zaten bitmişti, yani bu PID büyük olasılıkla BAŞKA bir şeridin `dotnet test` süreciydi. Kontrol ettiğimde süreç yoktu. Kill'den önce kendiliğinden bitmiş olabilir de, kill edilmiş de olabilir; hangisi olduğunu doğrulayamadım. O sırada test koşan bir şerit varsa sonucunu yeniden koşmalı.

## 11. Kurucuya / mali müşavire notlar

- Pozisyon bir iç yönetim göstergesidir, mevzuat hükmü içermez.
- Konsinye ve emanet malların pozisyon dışında tutulması muhasebe mantığına dayanır: mal bize ait değildir ya da sahibine borçluyuz. İstenirse mali müşavir bu tanımı teyit edebilir; kod bunu sabit bir mevzuat kuralı olarak iddia etmez.
