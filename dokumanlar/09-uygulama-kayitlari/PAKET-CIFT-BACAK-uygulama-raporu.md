# PAKET CIFT-BACAK — Uygulama raporu (30.09.2026, şerit t9 · tur 2)

Kapsam: sıfırdan müşteri senaryosunun 3 hatası (`ham/SENARYO-CIFT-BACAK-2026-09-30.md`).
- **HATA-1:** Gümüş emanet stornosu altın has kolonuna yazıyordu. Ayna da aynı kuralı kopyalıyordu.
- **HATA-2:** İade edilmiş emanetin girişi stornolanabiliyordu, emanet eksiye düşüyordu.
- **HATA-3:** Takasta alınan eski altın hurda bakiyesine girmiyordu.

Rol: Taşkın (backend) + Ertuğ (ekran, tur 2). COMMIT yapılmadı. Dosya silinmedi.

**Tur 2'de kapatılan hasım bulguları:**

| Bulgu | Önem | Kapanış |
|---|---|---|
| Takasın hurdaya giren eski has'ı ürün kartından türetiliyor; ürün düzenlenince geçmiş hurda kayıyor | KRİTİK | `hareketler.takas_eski_has_grami` kolonu (migration `TakasEskiHas`). Değer takas yazılırken donuyor, türetme ürün kartına bağlı değil. Hasım testi `Sinir_takas_sonrasi_urun_gram_duzeltmesi_gecmis_hurda_bakiyesini_degistirmez` yeşil. Canlıda Çeyrek gramı 2,5 / 1,0 / 1,75 yapıldı, hurda 10,076'da sabit kaldı. |
| Hurda kartındaki "giren" ile defter listesi uyuşmuyor | ORTA | Hurda defteri takasın eski bacağını «Takasta alınan» satırı olarak gösteriyor (eski has, tutar «—»). Canlıda defter girenlerinin toplamı 10,076 = kart girenHas 10,076. |
| İade edilmiş emanet girişinin «Geri al» düğmesi etkin kalıyor | DÜŞÜK | `Emanet.razor` düğmeyi önceden kilitliyor (`disabled`), sebebi `title` ve ayrıntı çekmecesinde yazıyor. Tarayıcıda 16/16 ölçümde kilitli. |

## 1. Yapılanlar

### HATA-1 — kural tek yerde (ekip oyu 1) · tur 1
- `EmanetModulu.cs` içinde `public static class EmanetKurali`:
  - `CariHasEtkisi(maden, hasGrami)` = `maden == ALTIN ? −hasGrami : 0`;
  - `CariHasKolonunaGirer(maden)`;
  - `EldeKalan(db, cariId, tenantId, maden, sube)`.
- Kuralı kullananlar:
  - **Yazan:** `EmanetServisi.Giris/Cikis`.
  - **Tersleyen:** `StornoServisi.CariEtkisi` (EMANET_* dalı).
  - **Ayna:** `CariServisi.CariHasEtkisi` ve `HasBakiyesineGirer`.
- Kural değişirse üçü birlikte değişir; ayna hatayı artık "doğru" sayamaz.

### HATA-2 — elde kalan kapısı (ekip oyu 2) · tur 1 + tur 2 ekran
- `StornoServisi.Storno`: kaynak `EMANET_GIRIS` ise `EmanetKurali.EldeKalan(cari, kaynak.Maden)` okunuyor. Değer `< kaynak.HasGrami` ise:
  - yanıt 409 **«Bu emanetin bir kısmı iade edildi; önce iadeyi geri alın.»**;
  - denetim `RedKaydet` ile yazılıyor.
- İade kapısı (`EmanetServisi.EmanetNet`) aynı `EldeKalan` hesabını kullanıyor.
- **Tur 2 — ekran (`Emanet.razor`):** `StornoAcik` artık `!IadeEdildi(h)` şartını da taşıyor.
  - `IadeEdildi` = giriş satırı, ters kaydı yok ve o madende elde kalan < girişin has'ı (sunucuyla aynı kural).
  - Kilitli düğmenin `title`'ı ve ayrıntı çekmecesindeki kilit satırı sebebi yazıyor. Metin `Sozlukler.CiftBacak.cs` (TR+EN) içinde ve sunucunun 409 metniyle aynı kaynaktan geliyor (`StornoServisi.EmanetIadeEdildiMesaji` = `CiftBacakSozlugu.Tr.EmanetIadeEdildiKilit`).
  - **Tur 2 canlı ölçümde bulunan açık (düzeltildi):** sunucu bakiyesi 0 olan madeni `madenBakiyeleri` listesine yazmıyor (`EmanetServisi`, `gram != 0` süzgeci). İlk sürümde listede olmayan altın dışı maden "bilinmiyor" sayılıyordu. Bu yüzden tam iadesi yapılmış gümüş girişinin düğmesi tarayıcıda **açık** kaldı (ilk ölçümde 8/8 `disabled:false`). Şimdi listede olmayan maden elde 0 sayılıyor; bu, iade formunun `SeciliMadenHas` kuralıyla aynı. bUnit testine sıfır bakiyeli PLATİN durumu eklendi (MCB-14).

### HATA-3 — takasın eski altın bacağı hurdada (ekip oyu 3) · tur 2'de kolona taşındı
- **`SatisModulu.Takas`** (yalnız takas bölgesi):
  - TAKAS satırına `Adet = dto.YeniUrunAdet`;
  - **tur 2:** `TakasEskiHasGrami = Yuvarla6(eskiHas)`. Müşteriden alınan eski has işlem anında satıra donuyor.
  - Net `has_grami` ve diğer kolonlar değişmedi.
- **`Entities.cs`:** `Hareket.TakasEskiHasGrami` (`takas_eski_has_grami decimal(15,6) NULL`). `null` değeri "bilinmiyor" anlamında.
- **`HurdaStokKapisi`:**
  - `TakasEskiBacaklari` artık yalnız kolondan okuyor, ürün join'i yok. Kolonu boş satırı saymıyor.
  - Kullananlar: `Bakiye` (çıkış kapısı `DogrulaCikis`), `Bakiyeler` (GİREN kalemi), `KiraciKirilimi` (pozisyon).
  - **Tur 2:** `DefterSorgu` (hurda türleri + eski has'ı bilinen TAKAS) ve `DefterHas` (TAKAS satırında eski has) eklendi.
- **`HurdaModulu.Defter`** (`HurdaStokServisi`): defter `DefterSorgu`'dan okunuyor. TAKAS satırının has'ı eski has, TL tutarı 0 (TL farkı satışın bedelidir, hurdanın değil).
- **`Hurda.razor`:** tür adı `TAKAS` → «Takasta alınan» (sözlükten, UX-S31). Tutar hücresinde «—» gösteriliyor.
- Verilen yeni ürün stoktan önceden olduğu gibi çıkıyor (değişmedi, testle sınandı).

## 2. Sapmalar ve gerekçeleri
1. **Tur 1'deki "türetme" kararı geri alındı (hasım KRİTİK).** Tur 1'de migration, KESIF-P2 şeridi de migration açıyor diye yazılmamıştı; bu gerekçe defter tutarlılığını askıya almaya yetmez. Tur 2'de kolon + geri doldurma yazıldı (§ 3).
2. **Kolonu boş (eski) TAKAS satırları hurdaya girmiyor.** Eski has uydurulmuyor (Anayasa md.1), bugünkü davranış (0) korunuyor. `kuyumcu_net`: 3 TAKAS satırının 3'ünde de `adet` ve `takas_eski_has_grami` NULL → etki 0 (§ 7).
3. **Madeni boş (K20 öncesi) emanet satırı** için ortak kural 0 veriyor (aynanın KESIF-P2 tanımı birebir). `kuyumcu_net`'te böyle satır yok.
4. **Dosya alanı dışındaki dokunuşlar (gerekçeli):**
   - `EmanetModulu.cs` giriş/çıkış satırları ortak kurala bağlandı ("ayna ile servis tek yardımcıyı paylaşsın").
   - `CariModulu.cs` aynanın iki satırı (`CariHasEtkisi`, `HasBakiyesineGirer`) aynı kuralın iki yüzü olduğu için birlikte değişti.
   - `Kuyumcu.Tests/Dogrulayici/KESIFP2SinirTestleri.cs`: eski (hatalı) kuralı sabitleyen 4 assert yeni kurala çevrildi, 1 assert eklendi (tur 1).
   - **Tur 2 (bulguların istediği):**
     - `Entities.cs` + migration (şema);
     - `HurdaModulu.cs` `Defter` (ORTA bulgu). Bu uç SUBE-YASAL23 / ŞUBE-KAPSAM-A şeridinin Hurda GET bölgesinde; yalnız sorgu kaynağı ve satır has'ı değişti, şube çözümü (`GorunumSubesi.Coz`) aynen korundu.
     - `Hurda.razor` (tür adı + tutar «—»);
     - `Emanet.razor` (DÜŞÜK bulgu).
   - Orkestratör kapısında bu iki uç/sayfa birleşimi gözle bakılmalı.
5. **Senaryo koşucusunun SQL aynası** (`senaryo.py:33`) eski hatalı kuralı kopyalıyordu. Ekip oyu 1'e göre düzeltildi (tur 1).
6. **Tarayıcı ölçümünde tema değeri:** tur 1 ölçüm betiği `light/dark` gönderiyordu; `tema.js` beyaz listesi `aydinlik/karanlik`. Bu yüzden tur 1 "açık" görüntüleri sistem temasıydı. Tur 2 ölçümü doğru değerlerle yeniden yapıldı ve görüntüler gözle doğrulandı (§ 7).

## 3. Migration
- Ad: **`20260930180743_TakasEskiHas`** (`dotnet/Kuyumcu.Api/Data/Migrations/`).
- Prosedür: önce `dotnet build` yeşil (`build1.log`), sonra `dotnet ef migrations add TakasEskiHas --project Kuyumcu.Api` (build'li, `--no-build` yok).
- Dosyalar: `.cs` 1650 bayt, `.Designer.cs` 243 919 bayt. Snapshot'ta `TakasEskiHasGrami` var.
- `Up()`:
  - `AddColumn takas_eski_has_grami decimal(15,6) NULL`;
  - geri doldurma: yalnız **adetli** TAKAS satırları için `has_grami + adet × urun.has_grami` bir kez dondurulur. Adetsiz satırlar NULL kalır.
- `Down()`: `DropColumn`.
- `dotnet ef database update … --connection "…password=$P"` (parola değişkenden) → «Applying migration '20260930180743_TakasEskiHas'. Done.» (`ef-upd.log`).
- Kanıt (`kuyumcu_net`, SELECT):
  - `__EFMigrationsHistory` son satır `20260930180743_TakasEskiHas`;
  - `show columns … like 'takas%'` → `takas_eski_has_grami decimal(15,6) YES NULL`;
  - TAKAS 3 satır: `adet` NULL 3, `takas_eski_has_grami` NULL 3 → geri doldurma 0 satır etkiledi.
- `kuyumcu_test_t9`'u testler kendisi kuruyor (EnsureDeleted + Migrate).

## 4. Testler
Yeni test dosyaları: `CiftBacakTestleri.cs` (19 durum: 18 DB/saf + 1 HTTP) ve `CiftBacakEkranTestleri.cs` (2, bUnit). Paketin toplamı **21 test** (tur 1'de 17).

| Test | HATA |
|---|---|
| GumusEmanetGirisStornosu_altin_has_kolonunu_degistirmez | 1 (5.5) |
| GumusEmanetIadeStornosu_altin_has_kolonunu_degistirmez | 1 (9.6) |
| AltinEmanetGirisStornosu_altin_kolonunu_tersler | 1 |
| Ayna_gumus_emanet_stornosunu_altin_saymaz | 1 |
| Ortak_kural_ayna_ile_ayni (×5) | 1 (saf) |
| IadeEdilmisEmanetinGirisStornosu_409_ve_hicbir_sey_yazilmaz (×2) | 2 (5.3, 9.3) |
| KismiIade_giris_stornosunu_engeller_iade_geri_alininca_acilir | 2 |
| Elde_kalan_kapisi_madenleri_karistirmaz | 2 |
| CiftBacakHttpTestleri.IadeEdilmisGumusEmanetGirisStornosu_HTTP_409_Turkce | 2 (JSON gövdesi ayrıştırılarak) |
| **CiftBacakEkranTestleri.Iadesi_yapilmis_emanet_girisinin_geri_al_dugmesi_onceden_kilitli_ve_sebebini_soyler** (tur 2) | 2 ekran: kısmi iade (ALTIN), sıfır bakiyeli maden (PLATİN) kilitli; karşılanan giriş, iade satırı, diğer maden açık; hiçbir storno isteği yok |
| **CiftBacakEkranTestleri.Ingilizce_kilit_metni_sozlukten_gelir** (tur 2) | TR+EN |
| Takas_eski_altin_hurda_bakiyesine_girer_yeni_urun_stoktan_cikar (tur 2: kolon 18,32 assert'i) | 3 (7.3) |
| Takas_eski_altini_hurda_satisinda_reddedilmez | 3 (Y1) |
| **Takas_eski_hasi_donar_urun_karti_duzeltilince_hurda_kaymaz** (tur 2) | 3: gram 10→12→8, bakiye / giren / pozisyon 18,32'de sabit |
| **Hurda_defteri_takas_satirini_eski_has_ile_gosterir_kart_girenle_tutar** (tur 2) | 3: defterde TAKAS 18,32 · TL 0 · toplam giren = kart giren |
| Adetsiz_eski_takas_satiri_hurdaya_girmez | 3 (kolonu boş eski satır) |

- **Hasım doğrulayıcı testleri** (`Dogrulayici/CIFTBACAKSinirTestleri.cs`, 9 test, dokunulmadı): 9/9 yeşil. `Sinir_takas_sonrasi_urun_gram_duzeltmesi_gecmis_hurda_bakiyesini_degistirmez` dahil.
- **Filtreli koşumlar** (`KUYUMCU_TEST_DB=kuyumcu_test_t9`):
  - `CiftBacak|CIFTBACAK`: 55/55;
  - `CiftBacak|CIFTBACAK|Emanet` (razor düzeltmesinden sonra): **217/217** yeşil.
- **Tam süit:**
  - ara koşum (razor sıfır-bakiye düzeltmesinden önce): 8131 · 0 başarısız;
  - **son kodla: 8152 · 8151 geçti · 1 atlandı · 0 başarısız** (`trx-pCIFT-BACAK/t3-son/`).

## 5. Mutasyon tablosu
Yöntem (`/private/tmp/kuyumcu-serit/CIFT-BACAK/mut2.sh`):
1. Kaynak `mut-yedek/CIFT-BACAK/t3-<kod>.*` altına kopyalandı.
2. `perl` ile TEK eşleşmeli değişiklik yapıldı; betik eşleşme sayısını sınar, 1 değilse atlar.
3. `dotnet test --filter "CiftBacak|CIFTBACAK|KESIFP2SinirTestleri|MadenDefteriTestleri"` (83 test) koşuldu.
4. Yedek geri kopyalandı ve `cmp` ile doğrulandı.

Günlük: `t3/mut-hepsi2.log`, `t3/mut-MCB-*.log`. Tur 2'de hepsi son kodla yeniden koşuldu.

| Kod | Değişiklik | Kırmızıya dönen testler (adıyla) | Geri alındı |
|---|---|---|---|
| MCB-1 | StornoServisi EMANET dalı → `-h.HasGrami` (eski hata) | Ayna_gumus_… · GumusEmanetGirisStornosu_… · GumusEmanetIadeStornosu_… · Sinir_emanet_cikis_stornosu_… | cmp AYNI |
| MCB-2 | Ayna → `storno \|\| Altin ? -has : 0` (eski hata) | Ayna_gumus_… · Ortak_kural(GUMUS,true) · Ortak_kural(PLATIN,true) · Sinir_sifir_eksi_… · KESIFP2 Sinir3 · Sinir4_emanet… · Sinir_has_bakiyesine_girmeyen… (7) | cmp AYNI |
| MCB-3 | `EmanetKurali.CariHasEtkisi` madene bakmaz | 12 test (Ayna, GumusGiris, GumusIade, Ortak_kural ×3, CIFTBACAK Sinir ×2, KESIFP2 ×4) | cmp AYNI |
| MCB-4 | Elde kalan kapısı devre dışı | HTTP_409_Turkce · Elde_kalan_… · IadeEdilmis…(GUMUS) · IadeEdilmis…(ALTIN) · KismiIade_… · Sinir_elde_kalan_4hane… | cmp AYNI |
| MCB-5 | Kapı `eldeKalan <= 0m` | KismiIade_… · Sinir_elde_kalan_4hane… | cmp AYNI |
| MCB-6 | `EldeKalan` maden süzgeci kaldırıldı | Elde_kalan_… · MadenDefteri ×3 | cmp AYNI |
| MCB-7 | Hurda `Bakiye` takası toplamaz | Takas_eski_altin_… · Takas_eski_altini_hurda_satisinda… · Takas_eski_hasi_donar… · Sinir_takas_adet2… · Sinir_takas_sonrasi_urun_gram… · Sinir_takas_stornosu… | cmp AYNI |
| MCB-8 | Eski has yerine NET has | Hurda_defteri_takas… · Takas_eski_altin_… · Takas_eski_altini_… · Takas_eski_hasi_donar… · Sinir ×3 | cmp AYNI |
| **MCB-9** | **Türetme yeniden ürün kartına bağlandı** (`has + adet × urunler.has_grami`, tur 1 hatası) | **Takas_eski_hasi_donar_urun_karti_duzeltilince_hurda_kaymaz · Sinir_takas_sonrasi_urun_gram_duzeltmesi_gecmis_hurda_bakiyesini_degistirmez** | cmp AYNI |
| MCB-10 | Takas yazarken `TakasEskiHasGrami = null` | Hurda_defteri… · Takas_eski_altin… · Takas_eski_altini… · Takas_eski_hasi_donar… · Sinir ×3. İlk koşum başka şeridin eşzamanlı derlemesiyle MSB3030 dosya kilidine takıldı; tek başına yeniden koşuldu (`mut-MCB-10.log`). | cmp AYNI |
| MCB-11 | Hurda `DefterSorgu` takası saymaz | Hurda_defteri_takas_satirini_eski_has_ile_gosterir_kart_girenle_tutar | cmp AYNI |
| MCB-12 | `DefterHas` takas için net has döner | Hurda_defteri_takas_satirini_… | cmp AYNI |
| MCB-13 | `Emanet.razor` `StornoAcik`'tan `!IadeEdildi` kaldırıldı | CiftBacakEkranTestleri.Iadesi_yapilmis_emanet_girisinin_geri_al_… | cmp AYNI |
| MCB-14 | Listede olmayan maden "bilinmiyor" sayılır (canlıda bulunan açık) | CiftBacakEkranTestleri.Iadesi_yapilmis_emanet_girisinin_geri_al_… | cmp AYNI |

- Sonuç: 14/14 kırmızı, test açığı yok.
- Son mutasyondan sonra temiz `dotnet build Kuyumcu.sln`: **0 Hata · 0 Uyarı** (`t3/build-temiz.log`).
- Arada bir derlemede 2 uyarı görüldü, ikisi de başka şeritlerin test dosyalarında: `SUBEKAPSAMASinirTestleri.cs:49` CS8601 ve `KesifP2Testleri.cs:451` xUnit2029.

## 6. denetim.sh
`bash dotnet/denetim.sh` → çıkış 0, **«DENETİM TEMİZ.»** (`t3/denetim.log`). Kilit sayaçlar:
- `ModalPencere kullanımı 47 (tavan 55)`;
- `STORNO_DISI_FINANSAL_PATCH 0`;
- `HURDA_STOK_KACAK_YOL 0`;
- `HURDA_FIRE_SIZINTISI 0`;
- `TEK_BACAK_STORNO_KAPISI 2`.

Tur 1'deki `ODEME_KALEMSIZ_YOL` ihlali (başka şeritlerin canlı verisi) artık yok.

## 7. Canlı doğrulama (API 5188 · vekil 5189 · CDP 9982 · `kuyumcu_test_t9`)
- Kaynak, yalıtım için `CIFT-BACAK/kopya/dotnet`'e rsync edildi. Değişen 10 dosya ağaçla `cmp` AYNI. Kopya derlendi (0/0) ve 5188'de açıldı.
- Razor düzeltmesinden sonra `Emanet.razor` yeniden kopyalandı (cmp AYNI), yeniden derlendi (0/0) ve API yeniden başlatıldı.
- Kiracı ürünün kendi uçlarıyla kuruldu: davet → kayıt → giriş → `POST /kur/guncelle` → 2 kasa, müşteri ve 3 ürün.
- ⚠ **Kendi hatam (tekrarlandı):** İlk canlı kiracı (`senaryo-3`) kurulduktan sonra filtreli testleri koştum. Test süreci `kuyumcu_test_t9`'u EnsureDeleted ile sildi ve kiracı gitti (sonraki girişte 401 «E-posta veya şifre hatalı»). Senaryo, sonda ve ölçüm yeni kiracı `PAKETCIFT-BACAK-TEST senaryo-4` (01M3SX0RB9A6N0WD16TFN25C92) ile **baştan** koşuldu. Aşağıdaki sonuçlar ikinci koşumundur, ilk koşum da aynı sonucu vermişti (`kosum-ana.log`, `sonda-t3.log`).

### Senaryo yeniden koşumu (46 adım) — `t3/kosum-ana-2.log`, `t3/kosum-ek-2.log`
- **Ana 40 adım:** 40 `[OK]` · 0 HATA · «SON değişmezler: TEMİZ · uyuşmazlık 0.0».
- **Ek 6 adım (9.1–9.6):** 6 `[OK]` · 0 HATA.

| Adım | Beklenen | Önce (30.09 senaryo) | Şimdi |
|---|---|---|---|
| 5.3 İade edilmiş gümüş giriş stornosu | 409, değişmez | 200, has kolonu +92,5, emanet gümüş −92,5 | **409**, Δ yok |
| 5.5 Gümüş giriş stornosu (iadesiz) | emanet gümüş −46,25 | + has kolonu +46,25 | **yalnız emanet gümüş −46,25** |
| 7.3 Takas | kasa1 +1.252 · Çeyrek −1 · hurda +2,748 | hurda Δ0 | **kasa1 +1.252 · Çeyrek −1 · hurda +2,748** |
| 9.3 İade edilmiş altın giriş stornosu | 409 | 200, has +9,16 | **409**, Δ yok |
| 9.6 Gümüş iade stornosu | emanet gümüş +18,5 | + has kolonu −18,5 | **yalnız emanet gümüş +18,5** |

Genel son değerler (beklenen = gerçek):
- TL 1.500 · has kolonu −4,58 · emanette 4,58;
- kasa1 93.472 · kasa2 103.752;
- stok Çeyrek 8;
- hurda 10,076.

### Tur 2 sondaları — `t3/sonda-t3-2.log`, `t3/sonda-y1.log`
- **K0:** `GET /hurda/bakiye` ALTIN → hasGram 10,076, girenHas 10,076.
  - SQL TAKAS: `has_grami 1.145000 · adet 1.000 · takas_eski_has_grami 2.748000`.
- **K1 (KRİTİK bulgu):** `PATCH /urunler/{Çeyrek}` art arda gram 2,5 → 1,0 → 1,75 (yanıt ve SQL `urunler.has_grami`: 2,29 → 0,916 → 1,603). Her adımdan sonra `/hurda/bakiye` → **hasGram 10,076 · girenHas 10,076** (kaymadı).
- **O1/O2 (ORTA bulgu):** `GET /hurda/defter` → `('TAKAS', 2.748, 0)`, `('HURDA_ALIS', 2.748, −2748)`, `('HURDA_ALIS', 4.58, −4580)`, toplamAdet 3. Defter giren toplamı 10,076 = kart girenHas 10,076 → **EŞİT**.
- **Y1:**
  - `POST /islemler/hurda-satis` 10,076 has → 200 (toplamTl 10.076, kalanHurdaHasGram 0);
  - ek 0,001 → **400** «Bu şubede yeterli hurda yok: elde 0 has gram altın var …»;
  - bakiye: giren 10,076 · çıkan 10,076 · has 0.

### Geçmiş veri etkisi (`kuyumcu_net`, yalnız SELECT, veri UPDATE yok)
- **TAKAS:** 3 satır (3 kiracı), üçünün de `adet` ve `takas_eski_has_grami` kolonu NULL. Hurda bakiyesi **değişmiyor**, geri doldurma 0 satırı etkiledi.
  - Adet 1 kabul edilseydi olacak etki (yalnız bilgi, uygulanmadı): 13,9232 / 13,9232 / 0,916 has.
- **HATA-1:** `kuyumcu_net`'te altın dışı emanet STORNO satırı yok. Ayna değişikliği hiçbir carinin uyuşmazlığını değiştirmiyor.
- **HATA-2:** (cari, maden) başına emanet neti eksi olan kayıt 0.

### Tarayıcı ölçümü (başsız Chrome, CDP 9982, `betikler/tur/cdp.mjs` + `sikisma-olc.js`)
- Betik: `t3/olc2.mjs`. Sonuçlar `t3/olcum/olcum.jsonl`, görüntüler `t3/olcum/foto/`. Tema değerleri `aydinlik/karanlik` (sapma 6).
- `/emanet`: «Senaryo Müşteri» seçilir, «Eski kayıtlar» açılır. 5.1 gümüş girişinin (92,5 has, gümüş elde 0) «Geri al» düğmesi okunur, sonra «Ayrıntı» çekmecesi açılır.
- `/hurda`: «Hurda defteri» açılır, takas satırı görünür alana kaydırılır.

| Rota | Gen. | Mod (istenen=gerçek) | Tema | Sonuç | K1 | K3 taşan/kırık | K5 TRY | Yatay | K9 | K2 % |
|---|---|---|---|---|---|---|---|---|---|---|
| /emanet | 1280 | BASİT | aydınlık | düğme `disabled` + title «Bu emanetin bir kısmı iade edildi; önce iadeyi geri alın.»; çekmecede «Bu kayıt geri alınamaz. …» + düğme kilitli | 0 | 0/0 | 0 | 0 | 6* | 98 |
| /emanet | 375 | BASİT | aydınlık | aynı | 0 | 0/0 | 0 | 0 | 6* | 97 |
| /emanet | 1280 | BASİT | karanlık | aynı | 0 | 0/0 | 0 | 0 | 6* | 98 |
| /emanet | 375 | BASİT | karanlık | aynı | 0 | 0/0 | 0 | 0 | 6* | 97 |
| /emanet | 1280 | PRO | aydınlık | aynı | 0 | 0/0 | 0 | 0 | 6* | 98 |
| /emanet | 375 | PRO | aydınlık | aynı | 0 | 0/0 | 0 | 0 | 6* | 97 |
| /emanet | 1280 | PRO | karanlık | aynı | 0 | 0/0 | 0 | 0 | 6* | 98 |
| /emanet | 375 | PRO | karanlık | aynı | 0 | 0/0 | 0 | 0 | 6* | 97 |
| /hurda | 1280 | BASİT | aydınlık | satır «30.09.2026 Takasta alınan Altın 2,748 g —» | 0 | 0/0 | 0 | 0 | 0 | 98 |
| /hurda | 375 | BASİT | aydınlık | aynı (kart düzeni) | 0 | 0/0 | 0 | 0 | 0 | 97 |
| /hurda | 1280 | BASİT | karanlık | aynı | 0 | 0/0 | 0 | 0 | 0 | 98 |
| /hurda | 375 | BASİT | karanlık | aynı | 0 | 0/0 | 0 | 0 | 0 | 97 |
| /hurda | 1280 | PRO | aydınlık | aynı | 0 | 0/0 | 0 | 0 | 0 | 98 |
| /hurda | 375 | PRO | aydınlık | aynı | 0 | 0/0 | 0 | 0 | 0 | 97 |
| /hurda | 1280 | PRO | karanlık | aynı | 0 | 0/0 | 0 | 0 | 0 | 98 |
| /hurda | 375 | PRO | karanlık | aynı | 0 | 0/0 | 0 | 0 | 0 | 97 |

\* K9 örnekleri: `td.metin-soluk "Geri alma: …" ⟂ #cekmece-emanet-hareket-baslik`, `⟂ dt "Tür"`. Bunlar ayrıntı çekmecesinin (sağdan açılan katman) altında kalan tablo hücreleri; iki içerik öğesinin çakışması değil.

Görüntüler Read ile incelendi:
- `emanet__BASIT_aydinlik_375.png`: kilitli soluk «Geri al» ve altında sebep tam, sarılmış.
- `emanet__PRO_karanlik_1280.png` (tur 2 ilk ölçüm, `…_dark_1280`): tablo + çekmece, taşma yok.
- `hurda__PRO_aydinlik_1280.png`: defterde «Takasta alınan · Altın · 2,748 g · —» satırı, «Hurda alış» satırlarıyla aynı hizada.
- `hurda__PRO_karanlik_375.png` (`…_dark_375`): kart düzeni, «Takasta alınan», tutar «—».

İlk ölçüm (razor düzeltmesinden önce, yanlış tema değeriyle) `olcum/olcum-yanlis-tema.jsonl` ve `olcum/hurda.jsonl` içinde saklı. O ölçüm gümüş girişin düğmesini **açık** buldu ve sıfır-bakiye düzeltmesini tetikledi (§ 1 HATA-2).

### Temizlik
- API (PID 59985, sarıcı 59974) ve vekil (PID 72950) `kill` ile durduruldu. `lsof` 5188, 5189 ve 9982 boş. 5199/5200'e dokunulmadı.
- `kuyumcu_test_t9` test kiracıları `zsh kiraci-temizle-t9.sh <id> --uygula` ile silindi: `PAKETCIFT%` 22 kiracı (senaryo-4 + birim testlerin bıraktıkları). Son durumda `PAKETCIFT%` 0, tenants 60 → 38.
  - Kalan 38 kiracı, kendi filtreli test koşumlarımın (Emanet kümesi) bıraktığı başka adlı test kiracıları. Bu veritabanı bu şeride özgü ve her test sürecinde EnsureDeleted ile sıfırlanıyor. `senaryo-3` kiracısı bu yolla zaten silinmişti.
- `kuyumcu_net`'e hiçbir satır yazılmadı (`PAKETCIFT%` 0; tenants 14; oturum başında 15 idi, fark başka şeritlerin).
- Sır dosyaları (`senaryo/kimlik.json`, `senaryo/sistem.anahtar`, `t3/fikstur.json`) `printf '' >` ile 0 bayta indirildi. Silme kurucuya bırakıldı.

## 8. DoD sözleşmesi
Yeni varlık türü yok, yeni uç yok. Yeni kolon (`takas_eski_has_grami`) bir TAKAS defter satırının değişmez alanı.

| Varlık | Düzenleme yolu | Not |
|---|---|---|
| Emanet hareketi (EMANET_GIRIS/CIKIS) | açıklama PATCH (P-12) · has/maden/ayar → storno (48-A) | İade edilmiş girişin stornosu 409. Ekranda düğme önceden kilitli, sebep ve doğru yol yazılı («önce iadeyi geri alın»). İade stornolanınca kilit kalkar (bUnit + DB testi). |
| TAKAS hareketi | storno KAPALI (ErtelenenTurler, değişmedi) | `adet` ve `takas_eski_has_grami` yazma anında donar. Geri alma yolunun yokluğu kapsam dışı (kuyrukta). Hurda defterinde satırın düzenleme eylemi yok; «Bu kayıt değiştirilemez» ipucu sayfada mevcut. |
| Hurda bakiyesi | türetme (kolon yok) | Takasın eski bacağı kolondan okunuyor. Defter listesi kartla aynı kaynaktan. |

Finansal varlıklarda hard DELETE yok, yeni ExecuteUpdate/Delete yok, `Hareketler.Add` yok. Migration içindeki geri doldurma bir şema adımıdır, çalışma zamanı yazma yolu değildir.

## 9. Dokunulan sayfalar
- `Kuyumcu.Web/Pages/Emanet.razor`: `StornoAcik` + `IadeEdildi` + `EldekiMaden`.
- `Kuyumcu.Web/Pages/Hurda.razor`: `TurAdi` TAKAS, tutar «—».

İkisi de tur 2'de bu paketin bulguları için açıldı. Başka dalgada açılırsa kapı kaydına gerekçe gerekir.

## 10. Değişen dosyalar (bu paket)
- `dotnet/Kuyumcu.Api/Modules/Emanet/EmanetModulu.cs`: `EmanetKurali`, giriş/çıkış ortak kural, `EmanetNet` delege.
- `dotnet/Kuyumcu.Api/Modules/Satis/StornoServisi.cs`: `CariEtkisi` EMANET dalı, elde kalan kapısı, `EmanetIadeEdildiMesaji`.
- `dotnet/Kuyumcu.Api/Modules/Cari/CariModulu.cs`: yalnız ayna (iki satır + yorum).
- `dotnet/Kuyumcu.Api/Modules/Satis/SatisModulu.cs`: yalnız Takas hareket satırı (`Adet`, `TakasEskiHasGrami`).
- `dotnet/Kuyumcu.Api/Modules/Hurda/HurdaStokKapisi.cs`: `TakasEskiBacaklari` (kolondan), `DefterSorgu`, `DefterHas`, `TakasSorgu`; `Bakiye`, `Bakiyeler`, `KiraciKirilimi`.
- `dotnet/Kuyumcu.Api/Modules/Hurda/HurdaModulu.cs`: yalnız `HurdaStokServisi.Defter`.
- `dotnet/Kuyumcu.Api/Data/Entities.cs`: `Hareket.TakasEskiHasGrami`.
- `dotnet/Kuyumcu.Api/Data/Migrations/20260930180743_TakasEskiHas.cs` + `.Designer.cs`, `KuyumcuDbContextModelSnapshot.cs`.
- `dotnet/Kuyumcu.Core/I18n/Sozlukler.CiftBacak.cs` (yeni): `EmanetIadeEdildiKilit`, `HurdaTurTakas` (TR+EN).
- `dotnet/Kuyumcu.Web/Pages/Emanet.razor`, `dotnet/Kuyumcu.Web/Pages/Hurda.razor`.
- `dotnet/Kuyumcu.Tests/CiftBacakTestleri.cs` (yeni), `dotnet/Kuyumcu.Tests/CiftBacakEkranTestleri.cs` (yeni).
- `dotnet/Kuyumcu.Tests/Dogrulayici/KESIFP2SinirTestleri.cs`: 4 assert güncellendi, 1 eklendi (tur 1, sapma 4).
- `dokumanlar/09-uygulama-kayitlari/PAKET-CIFT-BACAK-uygulama-raporu.md` (bu rapor).

Not: `git status` bu dosyaların birçoğunu izlenmeyen (`??`) dizinlerde gösteriyor (ağaç tek ilk commit'ten beri commit'siz).

## 11. Koşul karşılama

| Koşul | Durum | Kanıt |
|---|---|---|
| HATA-1 düzeltildi, ayna ile servis tek yardımcıyı paylaşıyor | KARŞILANDI | `EmanetKurali` · MCB-1/2/3 · senaryo 5.5 / 9.6 OK |
| HATA-2: 409 + Türkçe sebep, iade kapısıyla aynı toplam | KARŞILANDI | MCB-4/5/6 · 5.3 / 9.3 409 · HTTP testi |
| HATA-3: takas eski altını hurdaya girer, hurda satışı reddedilmez | KARŞILANDI | MCB-7/8/10 · 7.3 OK · Y1 200 |
| [KRİTİK] eski has değişmez saklanır, ürün kartına bağlı değil | KARŞILANDI | kolon + migration · MCB-9 · hasım testi yeşil · canlı K1 |
| [ORTA] defter = kart giren | KARŞILANDI | MCB-11/12 · canlı O2 EŞİT · /hurda 16 ölçüm |
| [DÜŞÜK] «Geri al» önceden kilitli + sebep | KARŞILANDI | MCB-13/14 · bUnit · /emanet 16 ölçüm |
| Geçmiş TAKAS satırları SQL ile sayıldı | KARŞILANDI | 3 satır, adet/eski has NULL → etki 0 |
| 46 adım yeniden koşuldu, HATA 0 | KARŞILANDI | ana 40 OK + ek 6 OK |
| Her kritik kural en az bir testi kırmızı yapıyor | KARŞILANDI | 14/14 kırmızı, hepsi cmp AYNI |
| build 0/0 | KARŞILANDI | `build-temiz.log` 0 Hata 0 Uyarı |
| denetim.sh TEMİZ | KARŞILANDI | «DENETİM TEMİZ.» |
| Tam süit yeni kırmızı yok | KARŞILANDI | son kodla **8152 · 8151 geçti · 1 atlandı · 0 başarısız** (`trx-pCIFT-BACAK/t3-son/_Mac_2026-09-30_22_51_13.trx`) |
| Ekran başsız Chrome 1280+375 · BASİT+PRO · aydınlık+karanlık | KARŞILANDI | § 7 tablo (16 ölçüm) + görüntüler |
| Test kiracısı silindi | KARŞILANDI | § 7 Temizlik |
| COMMIT yok · dosya silme yok · sır basılmadı | KARŞILANDI | — |

**Son kodla tam süit** (razor sıfır-bakiye düzeltmesinden sonra, temiz build 0 Hata 0 Uyarı `t3/build-son.log`): **Toplam 8152 · Geçti 8151 · Atlandı 1 · Başarısız 0** (`t3/tam-son.log`). Devir notundaki kırmızı listesiyle fark: 0. Süitin bıraktığı 21 `PAKETCIFT%` kiracısı da `kiraci-temizle-t9.sh --uygula` ile silindi; `PAKETCIFT%` 0.

## 12. Açık kalanlar
- **Başka şeritlere ait:**
  - build uyarıları (geçici): `Dogrulayici/SUBEKAPSAMASinirTestleri.cs:49` CS8601 (SUBE-KAPSAM-A) ve `KesifP2Testleri.cs:451` xUnit2029 (KESIF-P2);
  - `KesifP2Testleri.cs:92` bayat yorum (KESIF-P2).
- **Birleştirme notu:**
  - `HurdaModulu.cs` `Defter` (SUBE-YASAL23 / ŞUBE-KAPSAM-A bölgesi);
  - `CariModulu.cs` ayna (KESIF-P2 bölgesi);
  - migration `TakasEskiHas`, KESIF-P2'nin `UyrukAlpha3`'ünden sonra geliyor, snapshot ikisini de taşıyor.
- **Emanet `madenBakiyeleri` son 200 hareketten türetiliyor** (mevcut sunucu sınırı, değişmedi). 200'den eski hareketi olan carilerde ekran kilidi sunucudan farklı karar verebilir. Sunucu kapısı (409) belirleyici kalıyor, ekran yalnız ön uyarı.
- **Kapsam dışı (dokunulmadı, kuyrukta):** «düzeltme fişiyle giderin» metni (Sozlukler.KesifP2), salt altın veresiye 400, hurda alışı ve takasın geri alma yolu, TL aynası + denetim.sh bakiye kontrolü.

## 13. Kurucu / mali müşavir
Mevzuat hükmü yazılmadı. Değişiklikler iç defter tutarlılığıdır (emanet, has ve hurda bacakları); eşik, oran ya da vergi kuralı içermez. Takasta alınan eski altının hurda stokuna girmesi fiziki envanter kuralıdır ve ekip oyuyla (30.09 19:55) karara bağlandı.

Mali müşavire sorulabilecek tek konu: takasta alınan eski altının "hurda alışı" gibi gider pusulasıyla belgelenip belgelenmeyeceği. Bugün takas VUK 459 kapısına yalnız nakit farkla giriyor (`SatisModulu` yorumu: "mali müşavir teyidi beklemektedir"). Bu paket o davranışı değiştirmedi.
